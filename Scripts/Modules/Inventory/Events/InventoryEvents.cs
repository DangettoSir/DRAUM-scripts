using DRAUM.Core;
using UnityEngine;

namespace DRAUM.Modules.Inventory.Events
{
    /// <summary>
    /// Запрос на открытие инвентаря
    /// </summary>
    public class InventoryOpenRequestEvent : IEvent { }

    /// <summary>
    /// Запрос на закрытие инвентаря
    /// </summary>
    public class InventoryCloseRequestEvent : IEvent { }

    /// <summary>
    /// Событие открытия инвентаря
    /// </summary>
    public class InventoryOpenedEvent : IEvent { }

    /// <summary>
    /// Событие закрытия инвентаря
    /// </summary>
    public class InventoryClosedEvent : IEvent { }

    /// <summary>
    /// Событие добавления предмета в инвентарь
    /// </summary>
    public class ItemAddedEvent : IEvent
    {
        public ItemData ItemData { get; set; }
    }

    /// <summary>
    /// Событие удаления предмета из инвентаря
    /// </summary>
    public class ItemRemovedEvent : IEvent
    {
        public ItemData ItemData { get; set; }
    }

    /// <summary>
    /// Событие изменения инвентаря (добавление/удаление предметов)
    /// </summary>
    public class InventoryChangedEvent : IEvent { }
}



