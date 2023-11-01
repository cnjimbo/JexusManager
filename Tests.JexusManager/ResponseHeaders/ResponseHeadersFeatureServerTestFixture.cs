﻿// Copyright (c) Lex Li. All rights reserved.
// 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Tests.ResponseHeaders
{
    using System;
    using System.ComponentModel.Design;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using global::JexusManager.Features.ResponseHeaders;
    using global::JexusManager.Services;

    using Microsoft.Web.Administration;
    using Microsoft.Web.Management.Client;
    using Microsoft.Web.Management.Client.Win32;
    using Microsoft.Web.Management.Server;

    using Xunit;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using NSubstitute;

    public class ResponseHeadersFeatureServerTestFixture
    {
        private ResponseHeadersFeature _feature;

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

            var module = new ResponseHeadersModule();
            module.TestInitialize(_serviceContainer, null);

            _feature = new ResponseHeadersFeature(module);
            _feature.Load();
        }

        [Fact]
        public void TestBasic()
        {
            SetUp();
            Assert.Single(_feature.Items);
            Assert.Equal("X-Powered-By", _feature.Items[0].Name);
            Assert.Equal("ASP.NET", _feature.Items[0].Value);
        }

        [Fact]
        public void TestRemove()
        {
            SetUp();
            const string Expected = @"expected_remove.config";
            var document = XDocument.Load(Current);
            var node = document.Root.XPathSelectElement("/configuration/system.webServer/httpProtocol/customHeaders/add");
            node?.Remove();
            document.Save(Expected);

            _feature.SelectedItem = _feature.Items[0];
            _feature.Remove();
            Assert.Null(_feature.SelectedItem);
            Assert.Empty(_feature.Items);
            XmlAssert.Equal(Expected, Current);
        }

        [Fact]
        public void TestEdit()
        {
            SetUp();
            const string Expected = @"expected_edit.config";
            var document = XDocument.Load(Current);
            var node = document.Root.XPathSelectElement("/configuration/system.webServer/httpProtocol/customHeaders/add");
            node?.SetAttributeValue("value", "XSP");
            document.Save(Expected);

            _feature.SelectedItem = _feature.Items[0];
            var item = _feature.SelectedItem;
            item.Value = "XSP";
            _feature.EditItem(item);
            Assert.NotNull(_feature.SelectedItem);
            Assert.Equal("XSP", _feature.SelectedItem.Value);
            Assert.Single(_feature.Items);
            XmlAssert.Equal(Expected, Current);
        }

        [Fact]
        public void TestAdd()
        {
            SetUp();
            const string Expected = @"expected_add.config";
            var document = XDocument.Load(Current);
            var node = document.Root.XPathSelectElement("/configuration/system.webServer/httpProtocol/customHeaders/add");
            var add = new XElement("add",
                new XAttribute("name", "Server"),
                new XAttribute("value", "Jexus"));
            node?.AddAfterSelf(add);
            document.Save(Expected);

            var item = new ResponseHeadersItem(null);
            item.Name = "Server";
            item.Value = "Jexus";
            _feature.AddItem(item);
            Assert.NotNull(_feature.SelectedItem);
            Assert.Equal("Server", _feature.SelectedItem.Name);
            Assert.Equal(2, _feature.Items.Count);
            XmlAssert.Equal(Expected, Current);
        }
    }
}
