using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using CsvHelper;

namespace NamingPackages
{
    class Program
    {

        static void Main(string[] args)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            Console.WriteLine(currentDirectory);
            var allChecksPassed = true;

            if (!Directory.Exists($"{currentDirectory}/unpack"))
            {
                Directory.CreateDirectory($"{currentDirectory}/unpack");
                Console.WriteLine("unpack directory does not exist");
                allChecksPassed = false;
            }

            if (!Directory.Exists($"{currentDirectory}/output"))
            {
                Directory.CreateDirectory($"{currentDirectory}/output");
            }

            if (!Directory.Exists($"{currentDirectory}/config"))
            {
                Directory.CreateDirectory($"{currentDirectory}/config");
                Console.WriteLine("pattern.xml does not exist");
                allChecksPassed = false;
            }           
            
            if (!File.Exists($"{currentDirectory}/config/pattern.xml"))
            {
                File.WriteAllText($"{currentDirectory}/config/pattern.xml", "<patterns><pattern name=\"Name Of Zip\">example_%var_COMPANY%_%var_MONTH%_%var_MARKET%_%var_LAYOUT%</pattern></patterns>");
                Console.WriteLine("no pattern.xml - I created one for you");
                allChecksPassed = false;
            }

            if (Directory.Exists($"{currentDirectory}/temp"))
            {
                CleanUpTemp(currentDirectory);
            }
            else
            {
                Directory.CreateDirectory($"{currentDirectory}/temp");
            }

            if (allChecksPassed)
            {

                var patternXmlDocument = new XmlDocument();
                try
                {
                    patternXmlDocument.Load($"{currentDirectory}/config/pattern.xml");
                }
                catch
                {
                    Console.WriteLine("cannot parse pattern.xml - something wrong with xml");
                    Environment.Exit(0);
                }


                var zipDirectoryInfo = new DirectoryInfo($"{currentDirectory}/unpack/");

                var zipFiles = zipDirectoryInfo.GetFiles().Where(fileInfo => fileInfo.Name.Contains(".zip"));

                foreach (var zipFile in zipFiles)
                {
                    ConvertZipImages(zipFile, currentDirectory, patternXmlDocument);
                }

                CleanUpTemp(currentDirectory);
            }

            Console.WriteLine("Done");
            Console.ReadLine();

        }

        static void CleanUpTemp(string currentDirectory)
        {
            if (!Directory.Exists($"{currentDirectory}/temp"))
            {
                return;
            }
            
            var files = Directory.GetFiles($"{currentDirectory}/temp");

            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        static (string[], List<string>) GetPatternDetails(string patternText)
        {
            var patternSplitArray = patternText.Split("%");
            var variableList = new List<string>();

            for (int i = 0; i < patternSplitArray.Length; i++)
            {
                var subtext = patternSplitArray[i];
                
                if (subtext.Contains("var_"))
                {
                    patternSplitArray[i] = subtext.Substring(4);
                    variableList.Add(subtext.Substring(4));
                }
            }

            return (patternSplitArray, variableList);
        }
        
        static void ConvertZipImages(FileInfo zipFile, string currentDirectory, XmlDocument patternXmlDocument)
        {

            string csvPath = null;
            string fileName = zipFile.Name.Replace(".zip", "");
            
            Console.WriteLine($"processing {fileName}.zip");

            string patternString = null;
            foreach (XmlNode node in patternXmlDocument.SelectNodes("//patterns/pattern"))
            {
                if (node.Attributes["name"] != null)
                {
                    if (node.Attributes["name"].Value == fileName)
                    {
                        patternString = node.InnerText;
                        break;
                    }
                }
            };

            if (patternString == null)
            {
                Console.WriteLine($"cannot find pattern for {fileName}.zip --skipping");
                return;
            }

            var patternTuple = GetPatternDetails(patternString);

            var patternSplitArray = patternTuple.Item1;
            var variables = patternTuple.Item2;
            
            using (FileStream zipToOpen = new FileStream(zipFile.FullName, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.Contains(".csv"))
                        {
                            csvPath = $"{currentDirectory}/temp/{entry.FullName}";
                        }

                        entry.ExtractToFile($"{currentDirectory}/temp/{entry.FullName}");
                    }
                }
            }

            if (csvPath == null)
            {
                System.Environment.Exit(0);
            }

            var tempDirectoryInfo = new DirectoryInfo($"{currentDirectory}/temp/");
            var imageFiles = tempDirectoryInfo.GetFiles().Where(imageFile => !imageFile.Name.Contains(".csv")).ToList();

            if (Directory.Exists($"{currentDirectory}/output/{fileName}"))
            {
                var files = Directory.GetFiles($"{currentDirectory}/output/{fileName}");

                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            
            var outputDirectoryInfo = Directory.CreateDirectory($"{currentDirectory}/output/{fileName}");
            
            using (var reader = new StreamReader(csvPath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var recordsList = csv.GetRecords<dynamic>().ToList();

                for (int i = 0; i < recordsList.Count && i < imageFiles.Count; i++)
                {
                    var record = recordsList[i];
                    var imageFile = imageFiles[i];

                    string imageName = "";
                    
                    foreach (var strPattern in patternSplitArray)
                    {
                        if (variables.Contains(strPattern))
                        {
                            
                            foreach (var data in record)
                            {
                                if (data.Key == strPattern)
                                {
                                    imageName += data.Value;
                                }
                            }
                            
                            continue;
                        }

                        imageName += strPattern;
                    }

                    var extension = imageFile.Extension;
                    var fullName = outputDirectoryInfo.FullName + $"/{imageName}{extension}";

                    if (!File.Exists(fullName)) ;
                    
                    File.Copy(imageFile.FullName, fullName);
                }
                
            }
        }
    }
}
