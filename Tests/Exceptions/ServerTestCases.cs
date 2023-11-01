﻿// Copyright (c) Lex Li. All rights reserved.
// 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Tests.Exceptions
{
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using Microsoft.Web.Administration;
    using Xunit;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using System.Net;
    using System.Security.AccessControl;
    using System.Security.Principal;

    public class ServerTestCases
    {
        [Fact]
        public void Providers()
        {
            const string current = @"applicationHost.config";
            const string original = @"original2.config";
            const string originalMono = @"original.mono.config";
            File.Copy(Helper.IsRunningOnMono() ? originalMono : original, current, true);

            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }
#if IIS
            var server = new ServerManager(Path.Combine(directoryName, current));
#else
            var server = new IisExpressServerManager(Path.Combine(directoryName, current));
#endif
            var config = server.GetApplicationHostConfiguration();
            var section = config.GetSection("configProtectedData");
            Assert.Equal("RsaProtectedConfigurationProvider", section["defaultProvider"]);
            var collection = section.GetCollection("providers");
            Assert.Equal(5, collection.Count);
        }

        [Fact]
        public void MissingFile()
        {
            const string original = @"applicationHost.config";
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            var file = Path.Combine(directoryName, original);
            File.Delete(file);

#if IIS
            var server = new ServerManager(file);
#else
            var server = new IisExpressServerManager(file);
#endif
            var exception = Assert.Throws<FileNotFoundException>(
                () =>
                    {
                        TestCases.IisExpress(server, file);
                    });
            Assert.Equal(
                $"Filename: \\\\?\\{file}\r\nError: Cannot read configuration file\r\n\r\n",
                exception.Message);
        }

        [Fact]
        public void MissingClosingTag()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(@"original2_missing_closing.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var exception = Assert.Throws<COMException>(
                () =>
                    {
                        TestCases.IisExpress(server, current);
                    });
            Assert.Equal(
                $"Filename: \\\\?\\{current}\r\nLine number: 1134\r\nError: Configuration file is not well-formed XML\r\n\r\n",
                exception.Message);
        }

        [Fact]
        public void MissingRequiredAttribute()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // Remove the attribute.
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var pool = root.XPathSelectElement("/configuration/system.applicationHost/applicationPools/add[@name='UnmanagedClassicAppPool']");
                pool?.SetAttributeValue("name", null);
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var exception = Assert.Throws<COMException>(
                () =>
                    {
                        TestCases.IisExpress(server, current);
                    });
            Assert.Equal(
                $"Filename: \\\\?\\{current}\r\nLine number: 141\r\nError: Missing required attribute 'name'\r\n\r\n",
                exception.Message);
        }

        [Fact]
        public void ValidatorFails()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // Remove the attribute.
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var pool = root.XPathSelectElement("/configuration/system.applicationHost/applicationPools/add[@name='UnmanagedClassicAppPool']");
                pool?.SetAttributeValue("name", string.Empty);
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var exception = Assert.Throws<COMException>(
                () =>
                    {
                        TestCases.IisExpress(server, current);
                    });
            Assert.Equal(
                $"Filename: \\\\?\\{current}\r\nLine number: 141\r\nError: The 'name' attribute is invalid.  Invalid application pool name\r\n\r\n\r\n",
                exception.Message);
        }

        [Fact]
        public void InvalidAttribute()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // Remove the attribute.
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var pool = root.XPathSelectElement("/configuration/system.applicationHost/applicationPools/add[@name='UnmanagedClassicAppPool']");
                pool?.SetAttributeValue("testAuto", true);
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var exception = Assert.Throws<COMException>(
                () =>
                    {
                        TestCases.IisExpress(server, current);
                    });
            Assert.Equal(
                $"Filename: \\\\?\\{current}\r\nLine number: 141\r\nError: Unrecognized attribute 'testAuto'\r\n\r\n",
                exception.Message);
        }

        [Fact]
        public void ReadOnly()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            string siteConfig = TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            var message =
                "The configuration object is read only, because it has been committed by a call to ServerManager.CommitChanges(). If write access is required, use ServerManager to get a new reference.";
#if IIS
            var server = new ServerManager(true, current);
#else
            var server = new IisExpressServerManager(true, current);
#endif
            var exception1 = Assert.Throws<InvalidOperationException>(
                () =>
                    {
                        TestCases.IisExpress(server, current);
                    });
            Assert.Equal(message, exception1.Message);

            // site config "Website1"
            var config = server.Sites[0].Applications[0].GetWebConfiguration();

            // enable Windows authentication
            var windowsSection = config.GetSection("system.webServer/security/authentication/windowsAuthentication");
            Assert.Equal(OverrideMode.Inherit, windowsSection.OverrideMode);
            Assert.Equal(OverrideMode.Deny, windowsSection.OverrideModeEffective);
            Assert.True(windowsSection.IsLocked);
            Assert.False(windowsSection.IsLocallyStored);

            var windowsEnabled = (bool)windowsSection["enabled"];
            Assert.False(windowsEnabled);

            var compression = config.GetSection("system.webServer/urlCompression");
            Assert.Equal(OverrideMode.Inherit, compression.OverrideMode);
            Assert.Equal(OverrideMode.Allow, compression.OverrideModeEffective);
            Assert.False(compression.IsLocked);
            Assert.False(compression.IsLocallyStored);

            Assert.Equal(true, compression["doDynamicCompression"]);

            var compress = Assert.Throws<InvalidOperationException>(() => compression["doDynamicCompression"] = false);
            Assert.Equal(message, compress.Message);

            {
                // disable default document. Saved to web.config as this section can be overridden anywhere.
                ConfigurationSection defaultDocumentSection = config.GetSection("system.webServer/defaultDocument");
                Assert.Equal(true, defaultDocumentSection["enabled"]);

                ConfigurationElementCollection filesCollection = defaultDocumentSection.GetCollection("files");
                Assert.Equal(7, filesCollection.Count);

                {
                    var first = filesCollection[0];
                    Assert.Equal("home1.html", first["value"]);
                    Assert.True(first.IsLocallyStored);
                }

                var second = filesCollection[1];
                Assert.Equal("Default.htm", second["value"]);
                Assert.False(second.IsLocallyStored);

                var third = filesCollection[2];
                Assert.Equal("Default.asp", third["value"]);
                Assert.False(third.IsLocallyStored);

                var remove = Assert.Throws<FileLoadException>(() => filesCollection.RemoveAt(4));
#if IIS
                Assert.Equal(
                    "Filename: \r\nError: This configuration section cannot be modified because it has been opened for read only access\r\n\r\n",
                    remove.Message);
#else
                Assert.Equal(
                    $"Filename: \\\\?\\{siteConfig}\r\nError: This configuration section cannot be modified because it has been opened for read only access\r\n\r\n",
                    remove.Message);
#endif
                ConfigurationElement addElement = filesCollection.CreateElement();
                var add = Assert.Throws<InvalidOperationException>(() => filesCollection.AddAt(0, addElement));
                Assert.Equal(message, add.Message);

                Assert.Equal(7, filesCollection.Count);

                {
                    var first = filesCollection[0];
                    Assert.Equal("home1.html", first["value"]);
                    // TODO: why?
                    // Assert.IsFalse(first.IsLocallyStored);
                }

                Assert.Equal(7, filesCollection.Count);

                var clear = Assert.Throws<InvalidOperationException>(() => filesCollection.Clear());
                Assert.Equal(message, clear.Message);

                var delete = Assert.Throws<InvalidOperationException>(() => filesCollection.Delete());
                Assert.Equal(message, delete.Message);
            }
        }

        [Fact]
        public void NoBinding()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // change the path.
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var app = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='1']/bindings");
                app.Remove();
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var site = server.Sites[0];
            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                {
                    var binding = site.Bindings[0];
                });
        }


        [Fact]
        public void NoRootApplication()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // change the path.
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var app = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='1']/application");
                app?.SetAttributeValue("path", "/xxx");
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var site = server.Sites[0];
            var config = site.GetWebConfiguration();
            var exception = Assert.Throws<FileNotFoundException>(
                () =>
                {
                    var root = config.RootSectionGroup;
                });
#if IIS
            Assert.Equal(string.Format("Filename: \\\\?\\{0}\r\nError: Unrecognized configuration path 'MACHINE/WEBROOT/APPHOST/WebSite1'\r\n\r\n", current), exception.Message);
#else
            Assert.Equal(
                $"Filename: \\\\?\\{current}\r\nLine number: 154\r\nError: Unrecognized configuration path 'MACHINE/WEBROOT/APPHOST/WebSite1'\r\n\r\n", exception.Message);
#endif
        }

        [Fact]
        public void RootApplicationOutOfOrder()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var app = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='1']/application");
                app.AddBeforeSelf(
                    new XElement("application",
                        new XAttribute("path", "/xxx"),
                        new XElement("virtualDirectory",
                            new XAttribute("path", "/"),
                            new XAttribute("physicalPath", @"%JEXUS_TEST_HOME%\WebSite1"))));
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var site = server.Sites[0];
            var config = site.GetWebConfiguration();
            {
                var root = config.RootSectionGroup;
                Assert.NotNull(root);
            }

            // enable Windows authentication
            var windowsSection = config.GetSection("system.webServer/security/authentication/windowsAuthentication");
            Assert.Equal(OverrideMode.Inherit, windowsSection.OverrideMode);
            Assert.Equal(OverrideMode.Deny, windowsSection.OverrideModeEffective);
            Assert.True(windowsSection.IsLocked);
            Assert.False(windowsSection.IsLocallyStored);
        }

        [Fact]
        public void NoRootVirtualDirectory()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // modify the path
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var vDir = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='1']/application/virtualDirectory");
                vDir?.SetAttributeValue("path", "/xxx");
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var site = server.Sites[0];
            var config = site.GetWebConfiguration();
#if IIS
            var exception = Assert.Throws<NullReferenceException>(
                () =>
                {
                    var root = config.RootSectionGroup;
                });
#else
            var exception = Assert.Throws<FileNotFoundException>(
                () =>
                {
                    var root = config.RootSectionGroup;
                });
            Assert.Equal(
                $"Filename: \\\\?\\{current}\r\nLine number: 155\r\nError: Unrecognized configuration path 'MACHINE/WEBROOT/APPHOST/WebSite1'\r\n\r\n", exception.Message);
#endif
        }

        [Fact]
        public void RootVirtualDirectoryOutOfOrder()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // modify the path
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var vDir = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='1']/application/virtualDirectory");
                var newDir = new XElement("virtualDirectory",
                    new XAttribute("path", "/xxx"),
                    new XAttribute("physicalPath", @"%JEXUS_TEST_HOME%\WebSite1"));
                vDir.AddBeforeSelf(newDir);
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var site = server.Sites[0];
            var config = site.GetWebConfiguration();
            {
                var root = config.RootSectionGroup;
                Assert.NotNull(root);
            }

            // enable Windows authentication
            var windowsSection = config.GetSection("system.webServer/security/authentication/windowsAuthentication");
            Assert.Equal(OverrideMode.Inherit, windowsSection.OverrideMode);
            Assert.Equal(OverrideMode.Deny, windowsSection.OverrideModeEffective);
            Assert.True(windowsSection.IsLocked);
            Assert.False(windowsSection.IsLocallyStored);
        }

        [Fact]
        public void VirtualDirectoryDoesNotExist()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            var folder = @"X:\doesnotexist";
            {
                // modify the path
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var vDir = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='1']/application/virtualDirectory");
                vDir.SetAttributeValue("physicalPath", folder);
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var site = server.Sites[0];
            var config = site.GetWebConfiguration();
            var fileName = Path.Combine(folder, "web.config");
            var exception = Assert.Throws<DirectoryNotFoundException>(() => config.RootSectionGroup);
            Assert.Equal($"Filename: \\\\?\\{fileName}\r\nError: Cannot read configuration file\r\n\r\n", exception.Message);
        }

        [Fact]
        public void VirtualDirectoryInvalidPath()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            var folder = @"C:\Windows\*";
            {
                // modify the path
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var vDir = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='1']/application/virtualDirectory");
                vDir.SetAttributeValue("physicalPath", folder);
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var site = server.Sites[0];
            var config = site.GetWebConfiguration();
            var fileName = Path.Combine(folder, "web.config");
#if IIS
            var exception = Assert.Throws<FileNotFoundException>(() => config.RootSectionGroup);
#else
            var exception = Assert.Throws<DirectoryNotFoundException>(() => config.RootSectionGroup);
#endif
            Assert.Equal($"Filename: \\\\?\\{fileName}\r\nError: Cannot read configuration file\r\n\r\n", exception.Message);
        }

        [Fact]
        public void VirtualDirectoryInvalidPath2()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            var folder = @"C:\Windows\*t";
            {
                // modify the path
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var vDir = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='1']/application/virtualDirectory");
                vDir.SetAttributeValue("physicalPath", folder);
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var site = server.Sites[0];
            var config = site.GetWebConfiguration();
            var fileName = Path.Combine(folder, "web.config");
#if IIS
            var exception = Assert.Throws<FileNotFoundException>(() => config.RootSectionGroup);
#else
            var exception = Assert.Throws<DirectoryNotFoundException>(() => config.RootSectionGroup);
#endif
            Assert.Equal($"Filename: \\\\?\\{fileName}\r\nError: Cannot read configuration file\r\n\r\n", exception.Message);
        }

        [Fact]
        public void LogFileInheritance()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']");
                var log = new XElement("logFile",
                    new XAttribute("logFormat", "IIS"));
                site1?.Add(log);

                var site2 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='3']");
                var log2 = new XElement("logFile",
                    new XAttribute("directory", @"%IIS_USER_HOME%\Logs\1"));
                site2?.Add(log2);

                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                var site = server.Sites[0];
                Assert.Equal(@"%IIS_USER_HOME%\Logs", site.LogFile.Directory);
                Assert.Equal(LogFormat.W3c, site.LogFile.LogFormat);
            }

            {
                var site = server.Sites[1];
                Assert.Equal(@"%IIS_USER_HOME%\Logs", site.LogFile.Directory);
                Assert.Equal(LogFormat.Iis, site.LogFile.LogFormat);
            }

            {
                var site = server.Sites[2];
                Assert.Equal(@"%IIS_USER_HOME%\Logs\1", site.LogFile.Directory);
                Assert.Equal(LogFormat.W3c, site.LogFile.LogFormat);
            }
        }

        [Fact]
        public void DuplicateBindings()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']/bindings");
                site1?.Add(
                    new XElement("binding",
                        new XAttribute("protocol", "http"),
                        new XAttribute("bindingInformation", "*:61902:localhost")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                var exception = Assert.Throws<COMException>(() => server.Sites[1]);
                Assert.Equal($"Filename: \\\\?\\{current}\r\nLine number: 182\r\nError: Cannot add duplicate collection entry of type 'binding' with combined key attributes 'protocol, bindingInformation' respectively set to 'http, *:61902:localhost'\r\n\r\n", exception.Message);
            }
        }

        [Fact]
        public void BindingInvalidPort()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']/bindings");
                site1?.Add(
                    new XElement("binding",
                        new XAttribute("protocol", "http"),
                        new XAttribute("bindingInformation", "*:161902:localhost")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                Assert.Null(server.Sites[1].Bindings[2].EndPoint);
                Assert.Equal("*:161902:localhost", server.Sites[1].Bindings[2].BindingInformation);
            }
        }

        [Fact]
        public void BindingInvalidPort2()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']/bindings");
                site1?.Add(
                    new XElement("binding",
                        new XAttribute("protocol", "http"),
                        new XAttribute("bindingInformation", "*:localhost:localhost")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                Binding binding = server.Sites[1].Bindings[2];
                Assert.Null(binding.EndPoint);
                Assert.Equal("*:localhost:localhost", binding.BindingInformation);
#if !IIS
                var exception = Assert.Throws<ArgumentException>(() => binding.ToUri());
                Assert.Equal("Value does not fall within the expected range.", exception.Message);
#endif
            }
        }

        [Fact]
        public void BindingInvalidAddress()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']/bindings");
                site1?.Add(
                    new XElement("binding",
                        new XAttribute("protocol", "http"),
                        new XAttribute("bindingInformation", "1.1.1.1.1:61902:localhost")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                Binding binding = server.Sites[1].Bindings[2];
                Assert.Null(binding.EndPoint);
                Assert.Equal("1.1.1.1.1:61902:localhost", binding.BindingInformation);
#if !IIS
                Assert.Equal("http://localhost:61902", binding.ToUri());
                Assert.Equal("localhost on 1.1.1.1.1:61902 (http)", binding.ToShortString());
#endif
            }
        }

        [Fact]
        public void BindingInvalidAddress2()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']/bindings");
                site1?.Add(
                    new XElement("binding",
                        new XAttribute("protocol", "http"),
                        new XAttribute("bindingInformation", "1.1.1:61902:localhost")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                Binding binding = server.Sites[1].Bindings[2];
                Assert.Equal(IPAddress.Parse("1.1.0.1"), binding.EndPoint.Address);
                Assert.Equal("1.1.1:61902:localhost", binding.BindingInformation);
#if !IIS
                Assert.Equal("http://localhost:61902", binding.ToUri());
                Assert.Equal("localhost on 1.1.1:61902 (http)", binding.ToShortString());
#endif
            }
        }

        [Fact]
        public void BindingInvalidAddress3()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']/bindings");
                site1?.Add(
                    new XElement("binding",
                        new XAttribute("protocol", "http"),
                        new XAttribute("bindingInformation", ":")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                Binding binding = server.Sites[1].Bindings[2];
                Assert.Null(binding.EndPoint);
                Assert.Equal(":", binding.BindingInformation);
#if !IIS
                Assert.Equal("http://localhost", binding.ToUri());
                Assert.Equal(": (http)", binding.ToShortString());
#endif
            }
        }

        [Fact]
        public void BindingInvalidAddress4()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']/bindings");
                site1?.Add(
                    new XElement("binding",
                        new XAttribute("protocol", "http"),
                        new XAttribute("bindingInformation", "::")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                Binding binding = server.Sites[1].Bindings[2];
                Assert.Null(binding.EndPoint);
                Assert.Equal("::", binding.BindingInformation);
#if !IIS
                Assert.Equal("http://localhost", binding.ToUri());
                Assert.Equal(": (http)", binding.ToShortString());
#endif
            }
        }

        [Fact]
        public void BindingInvalidAddress5()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var site1 = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@id='2']/bindings");
                site1?.Add(
                    new XElement("binding",
                        new XAttribute("protocol", "http"),
                        new XAttribute("bindingInformation", "*:80:")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                Binding binding = server.Sites[1].Bindings[2];
                Assert.Equal(new IPEndPoint(IPAddress.Any, 80), binding.EndPoint);
                Assert.Equal("*:80:", binding.BindingInformation);
#if !IIS
                Assert.Equal("http://localhost", binding.ToUri());
                Assert.Equal("*:80 (http)", binding.ToShortString());
#endif
            }
        }

        [Fact]
        public void DuplicateApplicationPools()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var pools = root.XPathSelectElement("/configuration/system.applicationHost/applicationPools");
                pools?.Add(
                    new XElement("add",
                        new XAttribute("name", "Clr4IntegratedAppPool")));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                var exception = Assert.Throws<COMException>(() => server.ApplicationPools);
                Assert.Equal($"Filename: \\\\?\\{current}\r\nLine number: 143\r\nError: Cannot add duplicate collection entry of type 'add' with unique key attribute 'name' set to 'Clr4IntegratedAppPool'\r\n\r\n", exception.Message);
            }
        }

        [Fact]
        public void NoApplicationPools()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var pools = root.XPathSelectElement("/configuration/system.applicationHost/applicationPools");
                pools?.Remove();
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            {
                var pools = server.ApplicationPools;
                Assert.Empty(pools);
            }
        }

        [Fact]
        public void ConfigSource()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            var siteConfig = TestHelper.CopySiteConfig(directoryName, "original.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                root.Add(
                    new XElement("location",
                        new XAttribute("path", "WebSite1"),
                        new XElement("system.web",
                            new XElement("authorization",
                                new XAttribute("configSource", Path.Combine(Path.GetDirectoryName(siteConfig), "authorization.config"))))));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif

#if IIS
            var config = server.Sites[0].Applications[0].GetWebConfiguration();
            var exception = Assert.Throws<COMException>(() => config.GetSection("system.web/authorization"));
#else
            // TODO: fix where the exception is thrown.
            var exception = Assert.Throws<COMException>(() => server.Sites[0].Applications[0].GetWebConfiguration());
#endif
            Assert.Equal($"Filename: \\\\?\\{current}\r\nLine number: 1120\r\nError: Unrecognized attribute 'configSource'\r\n\r\n",
                exception.Message);
        }

        [Fact]
        public void InvalidLocation()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                root.Add(
                    new XElement("location",
                        new XAttribute("path", "NotExist"),
                        new XElement("system.webServer",
                            new XElement("security",
                                new XElement("authentication",
                                    new XElement("windowsAuthentication",
                                        new XAttribute("enabled", true)))))));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif

#if IIS
            var config = server.GetApplicationHostConfiguration();
            var exception = Assert.Throws<FileNotFoundException>(() => config.GetSection("system.webServer/security/authentication/windowsAuthentication", "NotExist"));
#else
            var config = server.GetApplicationHostConfiguration();
            var exception = Assert.Throws<FileNotFoundException>(() => config.GetSection("system.webServer/security/authentication/windowsAuthentication", "NotExist"));
            // TODO: fix where the exception is throwed.
            //var exception = Assert.Throws<COMException>(() => server.Sites[0].Applications[0].GetWebConfiguration());
#endif
            Assert.Equal($"Filename: \\\\?\\{current}\r\nError: Unrecognized configuration path 'MACHINE/WEBROOT/APPHOST/NotExist'\r\n\r\n",
                exception.Message);
        }

        [Fact]
        public void InvalidFileLocation()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                root.Add(
                    new XElement("location",
                        new XAttribute("path", "WebSite1/index2.html"),
                        new XElement("system.webServer",
                            new XElement("security",
                                new XElement("authentication",
                                    new XElement("windowsAuthentication",
                                        new XAttribute("enabled", true)))))));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif

            var config = server.GetApplicationHostConfiguration();
            var section = config.GetSection("system.webServer/security/authentication/windowsAuthentication", "WebSite1/index2.html");
            Assert.Equal(true, section["enabled"]);
        }

        [Fact]
        public void FileLocation()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                root.Add(
                    new XElement("location",
                        new XAttribute("path", "WebSite1/index.html"),
                        new XElement("system.webServer",
                            new XElement("security",
                                new XElement("authentication",
                                    new XElement("windowsAuthentication",
                                        new XAttribute("enabled", true)))))));
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif

            var config = server.GetApplicationHostConfiguration();
            var section = config.GetSection("system.webServer/security/authentication/windowsAuthentication", "WebSite1/index.html");
            Assert.Equal(true, section["enabled"]);
        }

        [Fact]
        public void Machine()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            string machine = Helper.FileNameMachineConfig;
            string backup = Path.GetTempFileName();
            File.Copy(machine, backup, true);
            try
            {

                {
                    // add the tags
                    var file = XDocument.Load(machine);
                    var root = file.Root;

                    var network = root?.XPathSelectElement("/configuration/system.net/mailSettings/smtp/network");
                    if (network == null)
                    {
                        var smtp = root?.XPathSelectElement("/configuration/system.net/mailSettings/smtp");
                        if (smtp == null)
                        {
                            var mailSettings = root?.XPathSelectElement("/configuration/system.net/mailSettings");
                            if (mailSettings == null)
                            {
                                var systemNet = root?.XPathSelectElement("/configuration/system.net");
                                if (systemNet == null)
                                {
                                    systemNet = new XElement("system.net");
                                    root?.Add(systemNet);
                                }

                                mailSettings = new XElement("mailSettings");
                                systemNet.Add(mailSettings);
                            }

                            smtp = new XElement("smtp",
                                new XAttribute("deliveryMethod", "Network"),
                                new XAttribute("from", "test@test.com"));
                            mailSettings.Add(smtp);
                        }

                        network = new XElement("network",
                            new XAttribute("defaultCredentials", true),
                            new XAttribute("host", "127.0.0.1"),
                            new XAttribute("port", 25),
                            new XAttribute("userName", "test"),
                            new XAttribute("password", "test"));
                        smtp.Add(network);
                    }

                    network.SetAttributeValue("enableSsl", false);
                    network.SetAttributeValue("something", "else");
                    file.Save(machine);
                }
#if IIS
                var server = new ServerManager(current);
#else
                var server = new IisExpressServerManager(current);
#endif
                var pools = server.ApplicationPools;

                var configuration = server.GetApplicationHostConfiguration();
                var section = configuration.GetSection("system.net/mailSettings/smtp");
                var element = section.GetChildElement("network");
                var exception = Assert.Throws<COMException>(() => element["enableSsl"]);

#if IIS
                Assert.Equal(
                    $"Invalid index. (Exception from HRESULT: 0x80070585)",
                    exception.Message);
#else
                Assert.Contains(
                    $"Filename: \\\\?\\{machine}\r\nLine number: ",
                    exception.Message);
                Assert.Contains("\r\nError: Unrecognized attribute 'enableSsl'\r\n\r\n", exception.Message);
#endif
            }
            finally
            {
                File.Copy(backup, machine, true);
            }
        }

        [Fact]
        public void Administration()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"administration.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var exception = Assert.Throws<COMException>(() => server.ApplicationPools);
            Assert.Equal(
                $"Filename: \\\\?\\{current}\r\nError: The configuration section 'system.applicationHost/applicationPools' cannot be read because it is missing a section declaration\r\n\r\n",
                exception.Message);
        }
#if !IIS
        [Fact]
        public void SchemaNonEmpty()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            string schemaIis = Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\IIS Express\config\schema\IIS_schema.xml");

            string backup = Path.GetTempFileName();
            File.Copy(schemaIis, backup, true);
            bool replaced = false;
            try
            {
                {
                    // add the tags
                    var file = XDocument.Load(schemaIis);
                    var root = file.Root;

                    var directory = root?.XPathSelectElement("/configSchema/sectionSchema[@name='system.applicationHost/log']/element[@name='centralW3CLogFile']/attribute[@name='directory']");
                    directory.SetAttributeValue("validationParameter", "");
                    file.Save(schemaIis);
                    replaced = true;
                }

                var server = new IisExpressServerManager(current);
                var pools = server.ApplicationPools;

                var configuration = server.GetApplicationHostConfiguration();
                var section = configuration.GetSection("system.applicationHost/log");
                var element = section.GetChildElement("centralW3CLogFile");
                var exception = Assert.Throws<COMException>(() => element["directory"] = "");

                Assert.Equal(
                    $"String must not be empty\r\n",
                    exception.Message);
            }
            finally
            {
                if (replaced)
                {
                    File.Copy(backup, schemaIis, true);
                }
            }
        }
#endif

        [Fact]
        public void WrongLockAttributes()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var httpErrors = root.XPathSelectElement("/configuration/system.webServer/httpErrors");
                httpErrors?.SetAttributeValue("lockAttributes", "notExisted");
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif

#if IIS
            var config = server.GetApplicationHostConfiguration();
            var exception = Assert.Throws<COMException>(() => config.GetSection("system.webServer/httpErrors"));
#else
            var exception = Assert.Throws<COMException>(() => server.GetApplicationHostConfiguration());
            // TODO: fix where the exception is throwed.
#endif
            Assert.Equal($"Filename: \\\\?\\{current}\r\nLine number: 364\r\nError: lockAttributes contains unknown attribute 'notExisted'\r\n\r\n",
                exception.Message);
        }

        [Fact]
        public void WildcardLockAttributes()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var httpErrors = root.XPathSelectElement("/configuration/system.webServer/httpErrors");
                httpErrors?.SetAttributeValue("lockAttributes", "*");
                file.Save(current);
            }
#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif

            var config = server.GetApplicationHostConfiguration();
            var section = config.GetSection("system.webServer/httpErrors");
        }

        [Fact]
        public void DecryptPasswordEmpty()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var anonymous = root.XPathSelectElement("/configuration/location[@path='WebSite2']/system.webServer/security/authentication/anonymousAuthentication");
                anonymous?.SetAttributeValue("password", "[enc:AesProvider::enc]");
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var config = server.GetApplicationHostConfiguration();

            {
                var section = config.GetSection("system.webServer/security/authentication/anonymousAuthentication", "WebSite2");
                var attribute = section["password"];
                Assert.Equal(string.Empty, attribute.ToString());
            }

            //var server = new IisExpressServerManager(current);
            //var exception = Assert.Throws<COMException>(() => server.GetApplicationHostConfiguration());
            //// TODO: fix where the exception is throwed.
            //Assert.Equal($"Filename: \\\\?\\{current}\r\nLine number: 364\r\nError: lockAttributes contains unknown attribute 'notExisted'\r\n\r\n",
            //    exception.Message);
        }

        [Fact]
        public void DecryptPasswordBroken()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var anonymous = root.XPathSelectElement("/configuration/location[@path='WebSite2']/system.webServer/security/authentication/anonymousAuthentication");
                anonymous?.SetAttributeValue("password", "[enc:AesProvider:enc]");
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var config = server.GetApplicationHostConfiguration();

            {
                var section = config.GetSection("system.webServer/security/authentication/anonymousAuthentication", "WebSite2");
                var attribute = section["password"];
                Assert.Equal("[enc:AesProvider:enc]", attribute.ToString());
            }

            //var server = new IisExpressServerManager(current);
            //var exception = Assert.Throws<COMException>(() => server.GetApplicationHostConfiguration());
            //// TODO: fix where the exception is throwed.
            //Assert.Equal($"Filename: \\\\?\\{current}\r\nLine number: 364\r\nError: lockAttributes contains unknown attribute 'notExisted'\r\n\r\n",
            //    exception.Message);
        }

        [Fact]
        public void DecryptPasswordBroken2()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var anonymous = root.XPathSelectElement("/configuration/location[@path='WebSite2']/system.webServer/security/authentication/anonymousAuthentication");
                anonymous?.SetAttributeValue("password", "[enc::enc]");
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var config = server.GetApplicationHostConfiguration();

            {
                var section = config.GetSection("system.webServer/security/authentication/anonymousAuthentication", "WebSite2");
                var attribute = section["password"];
                Assert.Equal("[enc::enc]", attribute.ToString());
            }

            //var server = new IisExpressServerManager(current);
            //var exception = Assert.Throws<COMException>(() => server.GetApplicationHostConfiguration());
            //// TODO: fix where the exception is throwed.
            //Assert.Equal($"Filename: \\\\?\\{current}\r\nLine number: 364\r\nError: lockAttributes contains unknown attribute 'notExisted'\r\n\r\n",
            //    exception.Message);
        }

        [Fact]
        public void DecryptPasswordBroken3()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var anonymous = root.XPathSelectElement("/configuration/location[@path='WebSite2']/system.webServer/security/authentication/anonymousAuthentication");
                anonymous?.SetAttributeValue("password", "[enc:enc]");
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var config = server.GetApplicationHostConfiguration();

            {
                var section = config.GetSection("system.webServer/security/authentication/anonymousAuthentication", "WebSite2");
                var attribute = section["password"];
                Assert.Equal("[enc:enc]", attribute.ToString());
            }

            //var server = new IisExpressServerManager(current);
            //var exception = Assert.Throws<COMException>(() => server.GetApplicationHostConfiguration());
            //// TODO: fix where the exception is throwed.
            //Assert.Equal($"Filename: \\\\?\\{current}\r\nLine number: 364\r\nError: lockAttributes contains unknown attribute 'notExisted'\r\n\r\n",
            //    exception.Message);
        }

        [Fact]
        public void DecryptPasswordBroken4()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var anonymous = root.XPathSelectElement("/configuration/location[@path='WebSite2']/system.webServer/security/authentication/anonymousAuthentication");
                anonymous?.SetAttributeValue("password", "[encenc]");
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var config = server.GetApplicationHostConfiguration();

            {
                var section = config.GetSection("system.webServer/security/authentication/anonymousAuthentication", "WebSite2");
                var attribute = section["password"];
                Assert.Equal("[encenc]", attribute.ToString());
            }

            //var server = new IisExpressServerManager(current);
            //var exception = Assert.Throws<COMException>(() => server.GetApplicationHostConfiguration());
            //// TODO: fix where the exception is throwed.
            //Assert.Equal($"Filename: \\\\?\\{current}\r\nLine number: 364\r\nError: lockAttributes contains unknown attribute 'notExisted'\r\n\r\n",
            //    exception.Message);
        }

        [Fact]
        public void DecryptPasswordBroken5()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var anonymous = root.XPathSelectElement("/configuration/location[@path='WebSite2']/system.webServer/security/authentication/anonymousAuthentication");
                anonymous?.SetAttributeValue("password", "[]");
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var config = server.GetApplicationHostConfiguration();

            {
                var section = config.GetSection("system.webServer/security/authentication/anonymousAuthentication", "WebSite2");
                var attribute = section["password"];
                Assert.Equal("[]", attribute.ToString());
            }

            //var server = new IisExpressServerManager(current);
            //var exception = Assert.Throws<COMException>(() => server.GetApplicationHostConfiguration());
            //// TODO: fix where the exception is throwed.
            //Assert.Equal($"Filename: \\\\?\\{current}\r\nLine number: 364\r\nError: lockAttributes contains unknown attribute 'notExisted'\r\n\r\n",
            //    exception.Message);
        }

        [Fact]
        public void DecryptPassword()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var config = server.GetApplicationHostConfiguration();
            {
                var section = config.GetSection("system.webServer/security/authentication/anonymousAuthentication");
                var attribute = section["password"];
                Assert.Equal("", attribute.ToString());
            }

            {
                var section = config.GetSection("system.webServer/security/authentication/anonymousAuthentication", "WebSite2");
                var attribute = section["password"];
                Assert.Equal(string.Empty, attribute.ToString());
            }
        }

        [Fact]
        public void InvalidSslFlags()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.SetEnvironmentVariable("JEXUS_TEST_HOME", directoryName);

            if (directoryName == null)
            {
                return;
            }

            string current = Path.Combine(directoryName, @"applicationHost.config");
            string original = Path.Combine(directoryName, @"original2.config");
            File.Copy(original, current, true);
            TestHelper.FixPhysicalPathMono(current);

            {
                // add the tags
                var file = XDocument.Load(current);
                var root = file.Root;
                if (root == null)
                {
                    return;
                }

                var binding = root.XPathSelectElement("/configuration/system.applicationHost/sites/site[@name='GuessMeWeb']/bindings/binding[@protocol='https']");
                binding?.SetAttributeValue("sslFlags", "NotExist");
                file.Save(current);
            }

#if IIS
            var server = new ServerManager(current);
#else
            var server = new IisExpressServerManager(current);
#endif
            var config = server.GetApplicationHostConfiguration();
            var exception = Assert.Throws<COMException>(() => server.Sites["GuessMeWeb"].Bindings[1].SslFlags);
            Assert.Equal($"Filename: \\\\?\\{current}\r\nLine number: 181\r\nError: The 'sslFlags' attribute is invalid.  Not a valid unsigned integer\r\n\r\n\r\n",
                exception.Message);
        }
    }
}
