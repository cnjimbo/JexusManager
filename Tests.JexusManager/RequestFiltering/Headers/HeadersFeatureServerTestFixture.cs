﻿// Copyright (c) Lex Li. All rights reserved.
// 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using System.Xml.XPath;

namespace Tests.RequestFiltering.Headers
{
    using System;
    using System.ComponentModel.Design;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using global::JexusManager.Features.RequestFiltering;
    using global::JexusManager.Services;

    using Microsoft.Web.Administration;
    using Microsoft.Web.Management.Client;
    using Microsoft.Web.Management.Client.Win32;
    using Microsoft.Web.Management.Server;
    using NSubstitute;
    using Xunit;

    public class HeadersFeatureServerTestFixture
    {
        private HeadersFeature _feature;

        private ServerManager _server;

        private ServiceContainer _serviceContainer;

        private const string Current = @"applicationHost.config";

        private void SetUp()
        {
            const string Original = @"original.config";
            const string OriginalMono = @"original.mono.config";
            if (Helper.IsRunningOnMono())
            {
                File.Copy("Website1/original.config", "Website1/web.config", true);
                File.Copy(OriginalMono, Current, true);
            }
            else
            {
                File.Copy("Website1\\original.config", "Website1\\web.config", true);
                File.Copy(Original, Current, true);
            }

            Environment.SetEnvironmentVariable(
                "JEXUS_TEST_HOME",
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            _server = new IisExpressServerManager(Current);

            _serviceContainer = new ServiceContainer();
            _serviceContainer.RemoveService(typeof(IConfigurationService));
            _serviceContainer.RemoveService(typeof(IControlPanel));
            var scope = ManagementScope.Server;
            _serviceContainer.AddService(typeof(IControlPanel), new ControlPanel());
            _serviceContainer.AddService(typeof(IConfigurationService),
                new ConfigurationService(null, _server.GetApplicationHostConfiguration(), scope, _server, null, null, null, null, null));

            _serviceContainer.RemoveService(typeof(IManagementUIService));
            var substitute = Substitute.For<IManagementUIService>();
            substitute.ShowMessage(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessageBoxButtons>(),
                Arg.Any<MessageBoxIcon>(),
                Arg.Any<MessageBoxDefaultButton>()).Returns(DialogResult.Yes);

            _serviceContainer.AddService(typeof(IManagementUIService), substitute);

            var module = new RequestFilteringModule();
            module.TestInitialize(_serviceContainer, null);

            _feature = new HeadersFeature(module);
            _feature.Load();
        }

        [Fact]
        public void TestBasic()
        {
            SetUp();
            Assert.Single(_feature.Items);
            Assert.Equal("test", _feature.Items[0].Header);
        }

        [Fact]
        public void TestRemove()
        {
            SetUp();
            const string Expected = @"expected_remove.config";
            var document = XDocument.Load(Current);
            var node = document.Root?.XPathSelectElement("/configuration/system.webServer/security/requestFiltering/requestLimits");
            node?.Remove();
            document.Save(Expected);

            _feature.SelectedItem = _feature.Items[0];
            _feature.Remove();
            Assert.Null(_feature.SelectedItem);
            Assert.Empty(_feature.Items);
            XmlAssert.Equal(Expected, Current);
        }

        [Fact]
        public void TestAdd()
        {
            SetUp();
            const string Expected = @"expected_add.config";
            var document = XDocument.Load(Current);
            var node = document.Root?.XPathSelectElement("/configuration/system.webServer/security/requestFiltering/requestLimits/headerLimits");
            node?.Add(
                new XElement("add",
                    new XAttribute("header", "test1"),
                    new XAttribute("sizeLimit", "200")));
            document.Save(Expected);

            var item = new HeadersItem(null);
            item.Header = "test1";
            item.SizeLimit = 200;
            _feature.AddItem(item);
            Assert.NotNull(_feature.SelectedItem);
            Assert.Equal("test1", _feature.SelectedItem.Header);
            Assert.Equal(2, _feature.Items.Count);
            XmlAssert.Equal(Expected, Current);
        }
    }
}
