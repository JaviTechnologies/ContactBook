using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace JaviTechnologies.ContactBook
{
    /// <summary>
    /// Manages the contact book.
    /// </summary>
    public class ContactBook : MonoBehaviour
    {
        // manager of the database
        [SerializeField]
        DataSource dataSource;

        // prefab to create the contact items from
        [SerializeField]
        RectTransform itemPrefab;

        // scroller to show the items
        [SerializeField]
        ScrollRect scrollRect;

        // handler of the item pool
        [SerializeField]
        ContentPool pool;

        // maximunm number of spawned items out of view, per side
        const int kMaxOutOfViewItems = 5;

        // maximun number of items to process per iteration
        const int kMaxItemsPerIteration = 7;

        /// <summary>
        /// Struct to keep the visual settings of the spawnable items
        /// </summary>
        struct TransformSetup
        {
            public Vector2 pivot;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 offsetMin;
            public Vector2 offsetMax;
            public Vector2 sizeDelta;
            public Vector2 anchoredPosition;    // to know the first item´s position
            public Vector2 size;

            /// <summary>
            /// Copies the settings from the blueprint.
            /// </summary>
            /// <param name="blueprint"></param>
            public void SetSetup(RectTransform blueprint)
            {
                pivot = blueprint.pivot;
                anchorMin = blueprint.anchorMin;
                anchorMax = blueprint.anchorMax;
                offsetMin = blueprint.offsetMin;
                offsetMax = blueprint.offsetMax;
                sizeDelta = blueprint.sizeDelta;
                anchoredPosition = blueprint.anchoredPosition;
                size = blueprint.rect.size;
            }

            /// <summary>
            /// Applies the settings to the given item.
            /// </summary>
            /// <param name="item"></param>
            public void ApplySetupToItem(RectTransform item)
            {
                item.pivot = pivot;
                item.anchorMin = anchorMin;
                item.anchorMax = anchorMax;
                item.offsetMin = offsetMin;
                item.offsetMax = offsetMax;
                item.sizeDelta = sizeDelta;
            }
        }

        // it keeps the settings of the spawnable items
        TransformSetup itemTransformSetup;

        // vertical spacing between items
        float verticalSpacing = 0f;

        // maximun position for spawnable items at the top
        float maxPositionTop = 0f;
        // minimun position for spawnable items at the bottom
        float minPositionBottom = 0f;

        // index of the last top item
        int lastTopItemId = -1;
        // index of the last bottom item
        int lastBottomItemId = -1;

        // current contacts to handle (after search)
        List<Contact> currentContacts = new();

        // current spawned items
        List<RectTransform> spawnedItems = new();

        // flag to know if we are ready to render the contact list
        private bool canRender = false;

        // flag to know if the scrolling list is currently updating
        private bool updating = false;

        /// <summary>
        /// Starts the behaviour.
        /// </summary>
        private async void Start()
        {
            // gather prefab setup
            itemTransformSetup.SetSetup(itemPrefab);

            // the vertical space is given by the setup in the prefab allocate in the content
            verticalSpacing = Mathf.Abs(itemTransformSetup.anchoredPosition.y);

            // calculate how many items fits in the viewport
            Vector2 viewportSize = scrollRect.viewport.rect.size;
            int viewportItemCount = (int) (viewportSize.y / itemTransformSetup.size.y);

            // calculate the top and bottom margings for the spawned items 
            maxPositionTop = kMaxOutOfViewItems * (verticalSpacing + itemTransformSetup.size.y);
            minPositionBottom = -(kMaxOutOfViewItems + viewportItemCount) * (verticalSpacing + itemTransformSetup.size.y);

            // hide prefab from the content
            itemPrefab.gameObject.SetActive(false);

            // wait until database loads
            await dataSource.WaitUntilReady();

            // get all contacts
            currentContacts = dataSource.GetAllContacts();

            // set content size
            Vector2 stepPosition = new Vector2(0f, verticalSpacing + itemTransformSetup.size.y);
            float contentVerticalSize = currentContacts.Count * stepPosition.y + verticalSpacing;
            scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, contentVerticalSize);

            // flag the initial data is ready
            canRender = true;
        }

        /// <summary>
        /// Updates the behaviour.
        /// </summary>
        private void Update()
        {
            if (!updating && canRender)
            {
                // update the scrolling list when the data is ready and it is not already updating
                UpdateList();
            }            
        }

        /// <summary>
        /// Searchs for contacts.
        /// </summary>
        /// <param name="searchText"></param>
        public async void SearchContacts(string searchText)
        {
            // flag data is not ready
            canRender = false;

            // TODO: show loading screen

            // clear content
            await ClearContent();

            // search
            currentContacts = await dataSource.SearchContactsAsync(searchText);

            // flag data is ready
            canRender = true;

            // TODO: hide loading screen
        }

        /// <summary>
        /// Clears all the items in the content.
        /// </summary>
        /// <returns></returns>
        private async Task ClearContent()
        {
            // removed items counter
            int removedItemsCount = 0;

            // remove all items
            while (spawnedItems.Count > 0)
            {
                // get last item
                int index = spawnedItems.Count - 1;
                var item = spawnedItems[index];

                // remove item from spawned items
                spawnedItems.RemoveAt(index);

                // pool removed item
                pool.PoolItem(item);

                // yield operation every X iterations
                ++removedItemsCount;
                if (removedItemsCount % 30 == 0)
                {
                    await Task.Yield();
                }
            }

            // reset indexes for the last items at the top and bottom
            lastTopItemId = -1;
            lastBottomItemId = -1;
        }

        /// <summary>
        /// Updates the scrolling list.
        /// </summary>
        private async void UpdateList()
        {
            // flag we are updating
            updating = true;

            if (currentContacts == null || currentContacts.Count == 0)
            {
                // return if no contacts to show
                updating = false;
                return;
            }

            // check if we need to put the first item
            if (lastTopItemId == -1)
            {
                Vector2 anchoredPosition = itemTransformSetup.anchoredPosition;
                var view = SpawnContactView(currentContacts[0], anchoredPosition);

                // register spawned item
                spawnedItems.Add(view);

                // update indexes for the last items at the top and bottom
                lastTopItemId = 0;
                lastBottomItemId = 0;
            }

            // update top and bottom sides
            var topTask = UpdateTop();
            var bottomTask = UpdateBottom();

            // wait for update tasks to finish
            await Task.WhenAll(topTask, bottomTask);

            // flag we are done updating
            updating = false;
        }

        /// <summary>
        /// Updates the top side of the scrolling list.
        /// </summary>
        /// <returns></returns>
        private async Task UpdateTop()
        {
            // get first item in the content
            var topItem = spawnedItems[0];

            // prepare flag to know if something changed
            var changed = false;
            // prepare counter for processed items per iteration
            var processedItems = 0;

            // check if we need to remove/pool items from the top
            while (!FitsSpawnableArea(topItem.anchoredPosition.y))
            {
                // remove item from spawned items
                spawnedItems.Remove(topItem);
                pool.PoolItem(topItem);

                // move counter for last top spawned item
                ++lastTopItemId;

                // flag that we changed the list
                changed = true;

                // update fisrt item
                topItem = spawnedItems[0];

                // yield if we processed kMaxItemsPerIteration already
                ++processedItems;
                if (processedItems % kMaxItemsPerIteration == 0)
                {
                    await Task.Yield();
                }
            }

            // return if we already changed the list
            if (changed)
            {
                return;
            }

            // displacement for each step
            Vector2 stepPosition = new Vector2(0f, verticalSpacing + itemTransformSetup.size.y);
            // current position of the top item
            Vector2 currentPosition = topItem.anchoredPosition;

            // check if we need to add items to the top
            while (lastTopItemId - 1 >= 0)
            {
                // move to next position
                var nextPosition = currentPosition + stepPosition;

                // break if the new item won´t fit the spawnable area
                if (!FitsSpawnableArea(nextPosition.y))
                {
                    break;
                }

                // spawn new item at the top
                var view = SpawnContactView(currentContacts[lastTopItemId - 1], nextPosition);
                spawnedItems.Insert(0, view);

                // move counter for the last top spawned item
                --lastTopItemId;

                // update current position
                currentPosition = view.anchoredPosition;

                // yield if we processed kMaxItemsPerIteration already
                ++processedItems;
                if (processedItems % kMaxItemsPerIteration == 0)
                {
                    await Task.Yield();
                }
            }
        }

        /// <summary>
        /// Updates the bottom side of the scrolling list.
        /// </summary>
        /// <returns></returns>
        private async Task UpdateBottom()
        {
            // get last item in the content
            var bottomItem = spawnedItems[spawnedItems.Count - 1];

            // prepare flag to know if something changed
            var changed = false;
            // prepare counter for processed items per iteration
            var processedItems = 0;

            // check if we need to remove/pool items from the bottom
            while (!FitsSpawnableArea(bottomItem.anchoredPosition.y))
            {
                // remove item from spawned items
                spawnedItems.Remove(bottomItem);
                pool.PoolItem(bottomItem);

                // move counter for last bottom spawned item
                --lastBottomItemId;

                // flag that we changed the list
                changed = true;

                // update last item in the content
                bottomItem = spawnedItems[spawnedItems.Count - 1];

                // yield if we processed kMaxItemsPerIteration already
                ++processedItems;
                if (processedItems % kMaxItemsPerIteration == 0)
                {
                    await Task.Yield();
                }
            }

            // return if we already changed the list
            if (changed)
            {
                return;
            }

            // check if we need to add items to the bottom
            Vector2 currentPosition = bottomItem.anchoredPosition;
            Vector2 stepPosition = new Vector2(0f, verticalSpacing + itemTransformSetup.size.y);
            while (lastBottomItemId + 1 < currentContacts.Count)
            {
                var nextPosition = currentPosition - stepPosition;

                // break if the new item won´t fit the spawnable area
                if (!FitsSpawnableArea(nextPosition.y))
                {
                    break;
                }

                // spawn new item at the bottom
                var view = SpawnContactView(currentContacts[lastBottomItemId + 1], nextPosition);
                spawnedItems.Add(view);

                // move counter for last bottom spawned item
                ++lastBottomItemId;

                // update current position
                currentPosition = view.anchoredPosition;

                // yield if we processed kMaxItemsPerIteration already
                ++processedItems;
                if (processedItems % kMaxItemsPerIteration == 0)
                {
                    await Task.Yield();
                }
            }
        }

        /// <summary>
        /// Checks if the given 'yPosition' fits in the spawnable area.
        /// </summary>
        /// <param name="yPosition"></param>
        /// <returns></returns>
        private bool FitsSpawnableArea(float yPosition)
        {
            // content container´s position
            var yContentPosition = scrollRect.content.anchoredPosition.y;

            // check if it is out of the bottom side
            // account for the position of the content container
            if (yPosition + yContentPosition < minPositionBottom)
            {
                return false;
            }

            // check if it is out of the top side
            // account for the height of the item and the position of the content container
            if (yPosition - itemTransformSetup.size.y + scrollRect.content.anchoredPosition.y > maxPositionTop)
            {
                return false;
            }

            // it is in the spawnable area
            return true;
        }

        /// <summary>
        /// Spawns a contact view with the given data at the given position
        /// </summary>
        /// <param name="contact"></param>
        /// <param name="anchoredPosition"></param>
        /// <returns></returns>
        private RectTransform SpawnContactView(Contact contact, Vector2 anchoredPosition)
        {
            var item = pool.GetOrCreateItem(itemPrefab);

            // first, set new parent
            item.SetParent(scrollRect.content);

            // set data
            var contactView = item.GetComponent<ContactView>();
            contactView.SetData(contact);

            // set visual parameters
            itemTransformSetup.ApplySetupToItem(item);

            // set position
            item.anchoredPosition = anchoredPosition;

            return item;
        }
    }
}
