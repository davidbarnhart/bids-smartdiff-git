using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;

namespace Smartdiff
{
    class Program
    {
        private static string[] DTS_FILE_EXTENSIONS = { ".dtsx" };
        private static string[] SSAS_FILE_EXTENSIONS = { ".dim", ".cube", ".dmm", ".dsv", ".bim" };
        private static string[] SSRS_FILE_EXTENSIONS = { ".rdl", ".rdlc" };

        static void Main(string[] args)
        {
            string FileNameA = args[0];
            string FileNameB = args[1];

            //get the XSLT for this file extension
            string sXslt = null;
            string sProjectItemFileName = FileNameA.ToLower();
            bool bNewLineOnAttributes = true;
            foreach (string extension in DTS_FILE_EXTENSIONS)
            {
                if (sProjectItemFileName.EndsWith(extension))
                {

                    sXslt = Smartdiff.Properties.Resources.SmartDiffDtsx;
                    break;
                }
            }
            foreach (string extension in SSAS_FILE_EXTENSIONS)
            {
                if (sProjectItemFileName.EndsWith(extension))
                {
                    sXslt = Smartdiff.Properties.Resources.SmartDiffSSAS;
                    break;
                }
            }
            foreach (string extension in SSRS_FILE_EXTENSIONS)
            {
                if (sProjectItemFileName.EndsWith(extension))
                {
                    sXslt = Smartdiff.Properties.Resources.SmartDiffSSRS;
                    bNewLineOnAttributes = false;
                    break;
                }
            }

            string tempFileA = System.IO.Path.GetTempFileName();
            string tempFileB = System.IO.Path.GetTempFileName();

            System.IO.File.Copy(FileNameA, tempFileA, true);
            System.IO.File.Copy(FileNameB, tempFileB, true);

            PrepXmlForDiff(tempFileA, sXslt, bNewLineOnAttributes);
            PrepXmlForDiff(tempFileB, sXslt, bNewLineOnAttributes);

            System.Diagnostics.Process.Start("CMD.exe", "/C WinmergeU.exe -e \"" + tempFileA + "\" \"" + tempFileB + "\"");

        }

        private static void PrepXmlForDiff(string sFilename, string sXSL, bool bNewLineOnAttributes)
        {
            System.IO.File.SetAttributes(sFilename, System.IO.FileAttributes.Normal); //unhide the file so you can overwrite it

            if (!string.IsNullOrEmpty(sXSL))
            {
                TransformXmlFile(sFilename, sXSL);
            }

            //format the XML file for easier visual diff
            XmlDocument doc = new XmlDocument();
            doc.Load(sFilename);
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = bNewLineOnAttributes;
            XmlWriter wr = XmlWriter.Create(sFilename, settings);
            doc.Save(wr);
            wr.Close();

            //finally, replace the &#xA; inside attributes with actual line breaks to make the SQL statements on Execute SQL tasks more readable
            StringBuilder sbReplacer = new StringBuilder(System.IO.File.ReadAllText(sFilename));
            sbReplacer.Replace("&#xD;&#xA;", "\r\n");
            sbReplacer.Replace("&#xD;", "\r\n");
            sbReplacer.Replace("&#xA;", "\r\n");
            System.IO.File.WriteAllText(sFilename, sbReplacer.ToString());
        }

        public static void TransformXmlFile(string xmlPath, string sXSL)
        {
            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
            System.IO.TextWriter writer = new System.IO.StreamWriter(memoryStream, new System.Text.UTF8Encoding());

            System.IO.StringReader xslReader = new System.IO.StringReader(sXSL);
            XmlReader xmlXslReader = XmlReader.Create(xslReader);

            XslCompiledTransform trans = new XslCompiledTransform();
            trans.Load(xmlXslReader);
            trans.Transform(xmlPath, null, writer);

            System.IO.File.WriteAllBytes(xmlPath, memoryStream.GetBuffer()); //can't write out to the input file until after the Transform is done
        }
    }
}
