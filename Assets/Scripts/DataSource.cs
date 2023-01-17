using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace JaviTechnologies.ContactBook
{
    /// <summary>
    /// Handles the source of the data and provides an API to search, remove
    /// and add contacts.
    /// </summary>
    public class DataSource : MonoBehaviour
    {
        // database to handle
        private ContactBookDatabase database;

        // helper to save and load the database
        private JsonFileHandler jsonFileHandler;

        // flag to indicate if the data is ready for use
        public bool IsReady { get; private set; }

        /// <summary>
        /// Starts the behaviour
        /// </summary>
        private void Start()
        {
            // prepare the database writer/reader
            jsonFileHandler = new JsonFileHandler(Application.persistentDataPath, DatabaseGenerator.kDatabaseFileName);

            // load database
            LoadDatabaseAsync();
        }

        /// <summary>
        /// Loads the database asynchronously
        /// </summary>
        private async void LoadDatabaseAsync()
        {
            // flag data is not ready
            IsReady = false;

            // load database asynchronously
            database = await jsonFileHandler.LoadAsync();

            if (database == null)
            {
                // create empty database if not found
                database = new ContactBookDatabase();
            }

            // flag data is ready now
            IsReady = true;
        }

        /// <summary>
        /// Waits until the data is ready to use
        /// </summary>
        /// <returns></returns>
        public async Task WaitUntilReady()
        {
            while (!IsReady)
            {
                await Task.Yield();
            }
        }

        /// <summary>
        /// Searches for contacts asynchronously.
        /// </summary>
        /// <param name="searchText"></param>
        /// <returns></returns>
        public async Task<List<Contact>> SearchContactsAsync(string searchText)
        {
            // prepare results
            List<Contact> results = new();

            if (!IsReady)
            {
                // return if data is not ready yet
                Debug.LogError("Database is not ready!");
                return results;
            }

            // convert to lower case
            searchText = searchText.ToLower();

            // search over all contacts
            for (int i = 0; i < database.entries.Count; ++i)
            {
                // contact
                Contact contact = database.entries[i];

                // search in first and last names
                if (contact.firstName.ToLower().Contains(searchText) || contact.lastName.ToLower().Contains(searchText))
                {
                    results.Add(contact);
                }

                // release execution every 50 iterations
                if (i % 50 == 0)
                {
                    await Task.Yield();
                }
            }

            return results;
        }

        /// <summary>
        /// Get all contacts.
        /// </summary>
        /// <returns></returns>
        public List<Contact> GetAllContacts()
        {
            if (IsReady)
            {
                return database.entries;
            }

            return new List<Contact>();
        }

        /// <summary>
        /// Adds a contact to the database.
        /// </summary>
        /// <param name="contact"></param>
        /// <returns></returns>
        public async Task AddContactAsync(Contact contact)
        {
            if (!IsReady)
            {
                // return if data is not ready yet
                Debug.LogError("Database is not ready!");
                return;
            }

            // add the contact
            database.entries.Add(contact);

            // order by name
            database.entries.Sort((x, y) => x.firstName.CompareTo(y.firstName));

            // save data
            await jsonFileHandler.SaveAsync(database);
        }

        /// <summary>
        /// Removes the given contact from the database.
        /// </summary>
        /// <param name="contact"></param>
        /// <returns></returns>
        public async Task RemoveContactAsync(Contact contact)
        {
            if (!IsReady)
            {
                // return if data is not ready yet
                Debug.LogError("Database is not ready!");
                return;
            }

            // remove the contact
            database.entries.Remove(contact);

            // save data
            await jsonFileHandler.SaveAsync(database);
        }
    }
}
