using System;
using UnityEditor;
using UnityEngine;

namespace JaviTechnologies.ContactBook
{
    /// <summary>
    /// Generates a json file with random contacts.
    /// The 'size' property indicates how many contacts will be generated.
    /// </summary>
    public class DatabaseGenerator
    {
        // name of the file (public because it is used outside)
        public static readonly string kDatabaseFileName = "ContactBook.datagame";
        // random generator
        private static readonly System.Random randomGenerator = new System.Random(DateTime.Now.Second);
        // number of contacts to generate
        private static readonly int size = 300;

        /// <summary>
        /// The main function that is called from the menu.
        /// </summary>
        [MenuItem("Tools/Generate contacts database")]
        static async void Generate()
        {
            Debug.Log("Generating contacts...");

            // prepare a json file handler
            JsonFileHandler jsonFileHandler = new JsonFileHandler(Application.persistentDataPath, kDatabaseFileName);

            // load the database if exists
            ContactBookDatabase database = await jsonFileHandler.LoadAsync();
            if (database == null)
            {
                // create a new one if it doesn´t exists
                database = new ContactBookDatabase();
            }

            // generate data
            PopulateDatabase(database);

            // save data
            Debug.Log("Saving contacts...");
            await jsonFileHandler.SaveAsync(database);

            Debug.LogFormat("Saved {0} contacts.", database.entries.Count);
        }

        /// <summary>
        /// Fills the given database with random contacts.
        /// </summary>
        /// <param name="database"></param>
        static void PopulateDatabase(ContactBookDatabase database)
        {
            // clear data
            if (database.entries == null)
                database.entries = new();
            else
                database.entries.Clear();

            // add random entries
            for (int i = 0; i < size; ++i)
            {
                Contact item = new();

                // name
                item.firstName = GenerateRandomName();
                item.lastName = GenerateRandomName();

                // score
                item.phone = GenerateRandomPhone();

                // add item
                database.entries.Add(item);
            }

            // order by name
            database.entries.Sort((x, y) => x.firstName.CompareTo(y.firstName));
        }

        /// <summary>
        /// Generates a random name.
        /// </summary>
        /// <returns>A random name</returns>
        /// <remarks>Taken from https://stackoverflow.com/questions/14687658/random-name-generator-in-c-sharp </remarks>
        static string GenerateRandomName()
        {
            string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
            string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
            string randomName = "";
            randomName += consonants[randomGenerator.Next(consonants.Length)].ToUpper();
            randomName += vowels[randomGenerator.Next(vowels.Length)];

            int minLength = 4;
            int maxLength = 7;
            int length = randomGenerator.Next(minLength, maxLength + 1);

            int b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
            while (b < length)
            {
                randomName += consonants[randomGenerator.Next(consonants.Length)];
                b++;
                randomName += vowels[randomGenerator.Next(vowels.Length)];
                b++;
            }

            return randomName;
        }

        /// <summary>
        /// Generates a random US phone number.
        /// </summary>
        /// <returns>A random US phone</returns>
        static string GenerateRandomPhone()
        {
            return string.Format("+1 {0} {1} {2}", GenerateRandomNumber(3), GenerateRandomNumber(3), GenerateRandomNumber(4));
        }

        /// <summary>
        /// Generates a random number of 'length' digits.
        /// </summary>
        /// <param name="length"></param>
        /// <returns>A random number</returns>
        static string GenerateRandomNumber(int length)
        {
            string number = "";

            for (int i = 0; i < length; ++i)
            {
                number += randomGenerator.Next(10);
            }

            return number;
        }
    }
}
