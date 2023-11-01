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

    public class ResponseHeadersFeatureSiteTestFixture
    {
        private ResponseHeadersFeature _feature;

        private ServerManager _server;

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

            var serviceContainer = new ServiceContainer();
            serviceContainer.RemoveService(typeof(IConfigurationService));
            serviceContainer.RemoveService(typeof(IControlPanel));
            var scope = ManagementScope.Site;
            serviceContainer.AddService(typeof(IControlPanel), new ControlPanel());
            serviceContainer.AddService(
                typeof(IConfigurationService),
                new ConfigurationService(
                    null,
                    _server.Sites[0].GetWebConfiguration(),
                    scope,
                    null,
                    _server.Sites[0],
                    null,
                    null,
                    null, _server.Sites[0].Name));

            serviceContainer.RemoveService(typeof(IManagementUIService));
            var substitute = Substitute.For<IManagementUIService>();
            substitute.ShowMessage(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<MessageBoxButtons>(),
                Arg.Any<MessageBoxIcon>(),
                Arg.Any<MessageBoxDefaultButton>()).Returns(DialogResult.Yes);

            serviceContainer.AddService(typeof(IManagementUIService), substitute);

            var module = new ResponseHeadersModule();
            module.TestInitialize(serviceContainer, null);

            _feature = new ResponseHeadersFeature(module);
            _feature.Load();
        }

        [Fact]
        public void TestBasic()
        {
            SetUp();
            Assert.Single(_feature.Items);
        }

        [Fact]
        public void TestRemoveInherited()
        {
            SetUp();

            var site = Path.Combine("Website1", "web.config");
            var expected = "expected_remove.site.config";
            var document = XDocument.Load(site);
            var node = document.Root.XPathSelectElement("/configuration/system.webServer");
            node?.Add(
                new XElement("httpProtocol",
                    new XElement("customHeaders",
                        new XElement("remove",
                            new XAttribute("name", "X-Powered-By")))));
            document.Save(expected);

            _feature.SelectedItem = _feature.Items[0];
            Assert.Equal("X-Powered-By", _feature.SelectedItem.Name);
            _feature.Remove();
            Assert.Null(_feature.SelectedItem);
            Assert.Empty(_feature.Items);

            const string Original = @"original.config";
            const string OriginalMono = @"original.mono.config";

            XmlAssert.Equal(Helper.IsRunningOnMono() ? OriginalMono : Original, Current);
            XmlAssert.Equal(expected, site);
        }

        [Fact]
        public void TestRemove()
        {
            SetUp();

            var site = Path.Combine("Website1", "web.config");
            var expected = "expected_remove1.site.config";
            var document = XDocument.Load(site);
            var node = document.Root.XPathSelectElement("/configuration/system.webServer");
            document.Save(expected);

            var item = new ResponseHeadersItem(null);
            item.Name = "Server";
            item.Value = "Jexus";
            _feature.AddItem(item);

            Assert.Equal("Server", _feature.SelectedItem.Name);
            Assert.Equal(2, _feature.Items.Count);
            _feature.Remove();
            Assert.Null(_feature.SelectedItem);
            Assert.Single(_feature.Items);

            const string Original = @"original.config";
            const string OriginalMono = @"original.mono.config";

            XmlAssert.Equal(Helper.IsRunningOnMono() ? OriginalMono : Original, Current);
            XmlAssert.Equal(expected, site);
        }

        [Fact]
        public void TestEditInherited()
        {
            SetUp();

            var site = Path.Combine("Website1", "web.config");
            var expected = "expected_edit.site.config";
            var document = XDocument.Load(site);
            var node = document.Root.XPathSelectElement("/configuration/system.webServer");
            node?.Add(
                new XElement("httpProtocol",
                    new XElement("customHeaders",
                        new XElement("remove",
                            new XAttribute("name", "X-Powered-By")),
                        new XElement("add",
                            new XAttribute("name", "X-Powered-By"),
                            new XAttribute("value", "XSP")))));
            document.Save(expected);

            _feature.SelectedItem = _feature.Items[0];
            Assert.Equal("X-Powered-By", _feature.SelectedItem.Name);
            var item = _feature.SelectedItem;
            item.Value = "XSP";
            _feature.EditItem(item);
            Assert.NotNull(_feature.SelectedItem);
            Assert.Equal("XSP", _feature.SelectedItem.Value);

            const string Original = @"original.config";
            const string OriginalMono = @"original.mono.config";

            XmlAssert.Equal(Helper.IsRunningOnMono() ? OriginalMono : Original, Current);
            XmlAssert.Equal(expected, site);
        }

        [Fact]
        public void TestEdit()
        {
            SetUp();

            var site = Path.Combine("Website1", "web.config");
            var expected = "expected_edit1.site.config";
            var document = XDocument.Load(site);
            var node = document.Root.XPathSelectElement("/configuration/system.webServer");
            node?.Add(
                new XElement("httpProtocol",
                    new XElement("customHeaders",
                        new XElement("add",
                            new XAttribute("name", "Server"),
                            new XAttribute("value", "Jexus2")))));
            document.Save(expected);

            var item = new ResponseHeadersItem(null);
            item.Name = "Server";
            item.Value = "Jexus";
            _feature.AddItem(item);

            Assert.Equal("Jexus", _feature.SelectedItem.Value);
            Assert.Equal(2, _feature.Items.Count);
            item.Value = "Jexus2";
            _feature.EditItem(item);
            Assert.NotNull(_feature.SelectedItem);
            Assert.Equal("Jexus2", _feature.SelectedItem.Value);
            Assert.Equal(2, _feature.Items.Count);

            const string Original = @"original.config";
            const string OriginalMono = @"original.mono.config";

            XmlAssert.Equal(Helper.IsRunningOnMono() ? OriginalMono : Original, Current);
            XmlAssert.Equal(expected, site);
        }

        [Fact]
        public void TestAdd()
        {
            SetUp();

            var site = Path.Combine("Website1", "web.config");
            var expected = "expected_edit.site.config";
            var document = XDocument.Load(site);
            var node = document.Root.XPathSelectElement("/configuration/system.webServer");
            node?.Add(
                new XElement("httpProtocol",
                    new XElement("customHeaders",
                        new XElement("add",
                            new XAttribute("name", "Server"),
                            new XAttribute("value", "Jexus")))));
            document.Save(expected);

            var item = new ResponseHeadersItem(null);
            item.Name = "Server";
            item.Value = "Jexus";
            _feature.AddItem(item);
            Assert.NotNull(_feature.SelectedItem);
            Assert.Equal("Server", _feature.SelectedItem.Name);

            const string Original = @"original.config";
            const string OriginalMono = @"original.mono.config";

            XmlAssert.Equal(Helper.IsRunningOnMono() ? OriginalMono : Original, Current);
            XmlAssert.Equal(expected, site);
        }
    }
}
