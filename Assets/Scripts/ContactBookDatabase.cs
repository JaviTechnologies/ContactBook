using System.Collections.Generic;

namespace JaviTechnologies.ContactBook
{
    /// <summary>
    /// Representation of the contact database.
    /// </summary>
    [System.Serializable]
    public class ContactBookDatabase
    {
        // list of contacts
        public List<Contact> entries;
    }
}

