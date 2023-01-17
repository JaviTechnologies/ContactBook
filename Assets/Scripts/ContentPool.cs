using System.Collections.Generic;
using UnityEngine;

namespace JaviTechnologies.ContactBook
{
    /// <summary>
    /// Manages the pooling of RectTransform items
    /// </summary>
    public class ContentPool : MonoBehaviour
    {
        // parent for the pooled items
        [SerializeField]
        RectTransform content;

        // list to keep the pooled items
        private List<RectTransform> pool = new();

        /// <summary>
        /// Adds an item to the pool
        /// </summary>
        /// <param name="item"></param>
        public void PoolItem(RectTransform item)
        {
            // pool item
            pool.Add(item);

            // inactivate the item
            item.gameObject.SetActive(false);

            // parent the item to the pool content
            item.SetParent(content);
        }

        /// <summary>
        /// Removes and return an item from the pool,
        /// or creates a new one from the given prefab.
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns>An item from the pool</returns>
        public RectTransform GetOrCreateItem(RectTransform prefab)
        {
            // prepare item to return
            RectTransform item = null;

            if (pool.Count == 0)
            {
                // create new item if pool is empty
                item = Instantiate<RectTransform>(prefab);
            }
            else
            {
                // get an item from the pool
                var index = pool.Count - 1;
                item = pool[index];
                pool.RemoveAt(index);
            }

            // activate the item before returning it
            item.gameObject.SetActive(true);

            return item;
        }
    }
}
