﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;

namespace CrazyKTV_WebUpdater
{
    public static class ControlExtentions
    {
        public static void MakeDoubleBuffered(this Control control, bool setting)
        {
            Type controlType = control.GetType();
            PropertyInfo pi = controlType.GetProperty("DoubleBuffered",
            BindingFlags.Instance | BindingFlags.NonPublic);
            pi.SetValue(control, setting, null);
        }

    }

    class CommonFunc
    {
        public static void CreateVersionXmlFile(string VersionFile)
        {

            XDocument xmldoc = new XDocument
                (
                    new XDeclaration("1.0", "utf-16", null),
                    new XElement("Configeruation")
                );
            xmldoc.Save(VersionFile);
        }

        public static List<string> LoadVersionXmlFile(string VersionFile, string FileName)
        {
            List<string> Value = new List<string>();
            Value.Add(FileName);
            try
            {
                XElement rootElement = XElement.Load(VersionFile);
                var Query = from childNode in rootElement.Elements("File")
                            where (string)childNode.Attribute("Name") == FileName
                            select childNode;

                foreach (XElement childNode in Query)
                {
                    Value.Add(childNode.Element("Ver").Value);
                    Value.Add(childNode.Element("Url").Value);
                    Value.Add(childNode.Element("Desc").Value);
                }
            }
            catch
            {
                Path.GetFileName(VersionFile);
                MessageBox.Show("【" + Path.GetFileName(VersionFile) + "】設定檔內容有錯誤,請刪除後再執行。");
            }
            return Value;
        }

        public static void SaveVersionXmlFile(string VersionFile, string FileName, string FileVer, string FileUrl, string FileDesc)
        {
            XDocument xmldoc = XDocument.Load(VersionFile);
            XElement rootElement = xmldoc.XPathSelectElement("Configeruation");

            var Query = from childNode in rootElement.Elements("File")
                        where (string)childNode.Attribute("Name") == FileName
                        select childNode;

            if (Query.ToList().Count > 0)
            {
                foreach (XElement childNode in Query)
                {
                    childNode.Element("Ver").Value = FileVer;
                    childNode.Element("Url").Value = FileUrl;
                    childNode.Element("Desc").Value = FileDesc;
                }
            }
            else
            {
                XElement AddNode = new XElement("File", new XAttribute("Name", FileName), new XElement("Ver", FileVer), new XElement("Url", FileUrl), new XElement("Desc", FileDesc));
                rootElement.Add(AddNode);
            }
            xmldoc.Save(VersionFile);
        }



    }
}
