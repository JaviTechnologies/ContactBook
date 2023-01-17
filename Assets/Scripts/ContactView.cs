using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JaviTechnologies.ContactBook
{
    /// <summary>
    /// Handles the visuals of the contact in the list
    /// </summary>
    public class ContactView : MonoBehaviour
    {
        [SerializeField]
        Image background;

        [SerializeField]
        Image profileImage;

        [SerializeField]
        TMP_Text contactName;

        [SerializeField]
        TMP_Text contactInitials;

        [SerializeField]
        TMP_Text phoneNumber;

        /// <summary>
        /// Fills the visuals from the contact data.
        /// </summary>
        /// <param name="contact"></param>
        internal void SetData(Contact contact)
        {
            contactName.text = string.Format("{0} {1}", contact.firstName, contact.lastName);
            contactInitials.text = string.Format("{0}{1}", contact.firstName[0], contact.lastName[0]);
            phoneNumber.text = contact.phone;
        }
    }
}
