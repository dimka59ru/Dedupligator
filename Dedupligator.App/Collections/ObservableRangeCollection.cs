using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Dedupligator.App.Collections
{
  /// <summary>
  /// ObservableCollection с поддержкой массовых операций без частых уведомлений.
  /// Уменьшает количество CollectionChanged событий при добавлении множества элементов.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class ObservableRangeCollection<T> : ObservableCollection<T>
  {
    /// <summary>
    /// Добавляет диапазон элементов. UI обновляется один раз через Reset.
    /// </summary>
    /// <param name="items">Элементы для добавления.</param>
    public void AddRange(IEnumerable<T> items)
    {
      ArgumentNullException.ThrowIfNull(items);

      var list = items.ToList();
      if (list.Count == 0) return;

      foreach (var item in list)
      {
        Items.Add(item);
      }

      // Один раз уведомляем, что содержимое изменилось
      OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Полностью заменяет содержимое коллекции.
    /// </summary>
    /// <param name="items">Новые элементы.</param>
    public void ReplaceWith(IEnumerable<T> items)
    {
      ArgumentNullException.ThrowIfNull(items);

      var list = items.ToList();

      if (Items.Count == 0 && list.Count == 0)
        return;

      Items.Clear();

      if (list.Count == 0)
      {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        return;
      }

      foreach (var item in list)
      {
        Items.Add(item);
      }

      OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Удаляет все элементы, удовлетворяющие условию.
    /// </summary>
    /// <param name="predicate">Условие удаления.</param>
    public void RemoveAll(Func<T, bool> predicate)
    {
      ArgumentNullException.ThrowIfNull(predicate);

      var removedItems = Items.Where(predicate).ToList();
      if (removedItems.Count == 0) return;

      foreach (var item in removedItems)
      {
        Items.Remove(item);
      }

      OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
  }
}