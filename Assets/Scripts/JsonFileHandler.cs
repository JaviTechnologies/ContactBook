using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace JaviTechnologies.ContactBook
{
    /// <summary>
    /// Handles the reading and writing of a json file.
    /// </summary>
    /// <remarks>Inspired by https://www.youtube.com/watch?v=aUi9aijvpgs tutorial</remarks>
    public class JsonFileHandler
    {
        // name of the directory where to save the file to
        private readonly string dataDirPath;
        // name for the file to save
        private readonly string dataFileName;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dirPath">Directory where to save the file</param>
        /// <param name="fileName">Name for the file to save</param>
        public JsonFileHandler(string dirPath, string fileName)
        {
            dataDirPath = dirPath;
            dataFileName = fileName;
        }

        /// <summary>
        /// Loads the database asynchronously.
        /// </summary>
        /// <returns>The database of contacts</returns>
        public async Task<ContactBookDatabase> LoadAsync()
        {
            // formulate the full path for the file
            string fullPath = Path.Combine(dataDirPath, dataFileName);

            // prepare the database to return
            ContactBookDatabase database = null;

            if (File.Exists(fullPath))
            {
                try
                {
                    // prepare the text to read
                    string jsonData = "";
                    using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            // read the file asynchronously
                            jsonData = await reader.ReadToEndAsync();
                        }
                    }

                    // parse the text as our database type
                    database = JsonUtility.FromJson<ContactBookDatabase>(jsonData);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }

            return database;
        }

        /// <summary>
        /// Saves the database asynchronously
        /// </summary>
        /// <param name="data"></param>
        /// <returns>An async Task</returns>
        public async Task SaveAsync(ContactBookDatabase data)
        {
            // formulate the full path for the file
            string fullPath = Path.Combine(dataDirPath, dataFileName);

            try
            {
                // create directory in case it does not exists yet
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                // parse our data as json text
                string jsonData = JsonUtility.ToJson(data, true);

                using (FileStream stream = new FileStream(fullPath, FileMode.Create))
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        // write the text asynchronously
                        await writer.WriteAsync(jsonData);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }
}
