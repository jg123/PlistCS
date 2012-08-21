using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using PlistCS;
using System.Threading.Tasks;
using Windows.System;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Testing
{
    [TestClass]
    public class plistTestsWinRT
    {
        StorageFolder baseLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
        string fixturesFolderName = "TestFixtures";
        string targetXmlPath = "targetXml.plist";
        string targetBinPath = "targetBin.plist";
        string sourceXmlPath = "testXml.plist";
        string sourceBinPath = "testBin.plist";
        string sourceImage   = "testImage.jpg";

        private async Task<StorageFolder> GetFixtureFolderAsync()
        {
            return await baseLocation.GetFolderAsync(fixturesFolderName);
        }

        private async Task<Dictionary<string, object>> CreateDictionaryAsync()
        {
            const int largeCollectionSize = 18;

            Dictionary<string, object> dict = new Dictionary<string, object>();
            Dictionary<string, object> largeDict = new Dictionary<string, object>();
            List<object> largeArray = new List<object>();

            for (int i = 0; i < largeCollectionSize; i++)
            {
                largeArray.Add(i);
                string key = i.ToString();
                if (i < 10)
                    key = "0" + i.ToString();
                largeDict.Add(key, i);
            }

            var imageFile = await (await GetFixtureFolderAsync()).GetFileAsync(sourceImage);
            using (var s = await imageFile.OpenStreamForReadAsync())
            {
                using (var br = new BinaryReader(s)) {
                    dict.Add("testImage", br.ReadBytes((int)br.BaseStream.Length));
                }
            }
            dict.Add("testDate", PlistDateConverter.ConvertFromAppleTimeStamp(338610664L));
            dict.Add("testInt", -3455);
            dict.Add("testDouble", 1.34223d);
            dict.Add("testBoolTrue", true);
            dict.Add("testBoolFalse", false);
            dict.Add("testString", "hello there");
            dict.Add("testArray", new List<object> { 34, "string item in array" });
            dict.Add("testArrayLarge", largeArray);
            dict.Add("testDict", new Dictionary<string, object> { { "test string", "inner dict item" } });
            dict.Add("testDictLarge", largeDict);

            return dict;
        }

        private async Task CheckDictionaryAsync(Dictionary<string, object> dict)
        {
            Dictionary<string, object> actualDict = await CreateDictionaryAsync();
            Assert.AreEqual(dict["testDate"], actualDict["testDate"], "Dates do not correspond.");
            Assert.AreEqual(dict["testInt"], actualDict["testInt"], "Integers do not correspond.");
            Assert.AreEqual(dict["testDouble"], actualDict["testDouble"], "Reals do not correspond.");
            Assert.AreEqual(dict["testBoolTrue"], actualDict["testBoolTrue"], "BoolTrue's do not correspond.");
            Assert.AreEqual(dict["testBoolFalse"], actualDict["testBoolFalse"], "BoolFalse's do not correspond.");
            Assert.AreEqual(dict["testString"], actualDict["testString"], "Dates do not correspond.");
            CollectionAssert.AreEquivalent((byte[])dict["testImage"], (byte[])actualDict["testImage"], "Images do not correspond");
            CollectionAssert.AreEquivalent((List<object>)dict["testArray"], (List<object>)actualDict["testArray"], "Arrays do not correspond");
            CollectionAssert.AreEquivalent((List<object>)dict["testArrayLarge"], (List<object>)actualDict["testArrayLarge"], "Large arrays do not correspond.");
            CollectionAssert.AreEquivalent((Dictionary<string, object>)dict["testDict"], (Dictionary<string, object>)actualDict["testDict"], "Dictionaries do not correspond.");
            CollectionAssert.AreEquivalent((Dictionary<string, object>)dict["testDictLarge"], (Dictionary<string, object>)actualDict["testDictLarge"], "Large dictionaries do not correspond.");
        }

        private void waitTaskCompletion(Task task)
        {
            while (!task.IsCompleted) {
                // busy loop.
            }
            if (task.Exception != null) {
                throw task.Exception.InnerException;
            }
        }

        [TestMethod]
        public void ReadBinary()
        {
            waitTaskCompletion(ReadBinaryAsync());
        }

        private async Task ReadBinaryAsync()
        {
            var f = await (await GetFixtureFolderAsync()).GetFileAsync(sourceBinPath);
            await CheckDictionaryAsync((Dictionary<string, object>)(await Plist.readPlistAsync(f.Path)));
        }

        [TestMethod]
        public void ReadXml()
        {
            waitTaskCompletion(ReadXmlAsync());
        }

        private async Task ReadXmlAsync()
        {
            var f = await (await GetFixtureFolderAsync()).GetFileAsync(sourceXmlPath);
            await CheckDictionaryAsync((Dictionary<string, object>)(await Plist.readPlistAsync(f.Path)));
        }

        [TestMethod]
        public void WriteBinary()
        {
            waitTaskCompletion(WriteBinaryAsync());
        }

        private async Task WriteBinaryAsync()
        {
            var f = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, targetBinPath);
            await Plist.writeBinaryAsync(await CreateDictionaryAsync(), f);
            await CheckDictionaryAsync((Dictionary<string, object>)(await Plist.readPlistAsync(f)));
        }

        [TestMethod]
        public void WriteXml()
        {
            var t = WriteXmlAsync();
            waitTaskCompletion(t);
        }

        private async Task WriteXmlAsync()
        {
            var f = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, targetXmlPath);
            await Plist.writeXmlAsync(await CreateDictionaryAsync(), f);
            await CheckDictionaryAsync((Dictionary<string, object>)(await Plist.readPlistAsync(f)));
        }

        [TestMethod]
        public void ReadWriteBinaryByteArray()
        {
            var t = ReadWriteBinaryByteArrayAsync();
            waitTaskCompletion(t);
        }

        private async Task ReadWriteBinaryByteArrayAsync()
        {
            await CheckDictionaryAsync((Dictionary<string, object>)Plist.readPlist(Plist.writeBinary(await CreateDictionaryAsync())));
        }
    }
}