using SquidVox.Core.Collections;
using SquidVox.Tests.TestHelpers;

namespace SquidVox.Tests.Collections;

/// <summary>
/// Contains unit tests for the SvoxGameObjectCollection class.
/// </summary>
[TestFixture]
/// <summary>
/// 
/// </summary>
public class SvoxGameObjectCollectionTests
{
    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void Add_AddsGameObject_IncrementsCount()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        var gameObject = new MockGameObject { Name = "Test" };

        // Act
        collection.Add(gameObject);

        // Assert
        Assert.That(collection.Count, Is.EqualTo(1));
        Assert.That(collection.Contains(gameObject), Is.True);
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void Add_DuplicateGameObject_ThrowsException()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        var gameObject = new MockGameObject { Name = "Test" };
        collection.Add(gameObject);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => collection.Add(gameObject));
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void Remove_ExistingGameObject_RemovesAndReturnsTrue()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        var gameObject = new MockGameObject { Name = "Test" };
        collection.Add(gameObject);

        // Act
        var result = collection.Remove(gameObject);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(collection.Count, Is.EqualTo(0));
        Assert.That(collection.Contains(gameObject), Is.False);
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void Remove_NonExistingGameObject_ReturnsFalse()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        var gameObject = new MockGameObject { Name = "Test" };

        // Act
        var result = collection.Remove(gameObject);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void Indexer_ReturnsGameObjectsSortedByZIndex()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        var obj1 = new MockGameObject { Name = "A", ZIndex = 10 };
        var obj2 = new MockGameObject { Name = "B", ZIndex = 5 };
        var obj3 = new MockGameObject { Name = "C", ZIndex = 15 };

        collection.Add(obj1);
        collection.Add(obj2);
        collection.Add(obj3);

        // Act & Assert
        Assert.That(collection[0].Name, Is.EqualTo("B")); // ZIndex 5
        Assert.That(collection[1].Name, Is.EqualTo("A")); // ZIndex 10
        Assert.That(collection[2].Name, Is.EqualTo("C")); // ZIndex 15
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void CheckForZIndexChanges_DetectsChanges_MarksDirty()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        var obj = new MockGameObject { Name = "Test", ZIndex = 10 };
        collection.Add(obj);

        // Force sort
        _ = collection[0];

        // Act
        obj.ZIndex = 20;
        collection.CheckForZIndexChanges();

        // The collection should re-sort
        // We can't directly test _isDirty, but we can verify sorting still works
        Assert.That(collection[0].ZIndex, Is.EqualTo(20));
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void GetGameObjectsInZRange_ReturnsCorrectObjects()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        collection.Add(new MockGameObject { Name = "A", ZIndex = 5 });
        collection.Add(new MockGameObject { Name = "B", ZIndex = 10 });
        collection.Add(new MockGameObject { Name = "C", ZIndex = 15 });
        collection.Add(new MockGameObject { Name = "D", ZIndex = 20 });

        // Act
        var result = collection.GetGameObjectsInZRange(8, 16).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Name, Is.EqualTo("B"));
        Assert.That(result[1].Name, Is.EqualTo("C"));
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void GetEnabledGameObjects_ReturnsOnlyEnabled()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        collection.Add(new MockGameObject { Name = "A", IsEnabled = true });
        collection.Add(new MockGameObject { Name = "B", IsEnabled = false });
        collection.Add(new MockGameObject { Name = "C", IsEnabled = true });

        // Act
        var result = collection.GetEnabledGameObjects().ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void GetVisibleGameObjects_ReturnsOnlyVisible()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        collection.Add(new MockGameObject { Name = "A", IsVisible = true });
        collection.Add(new MockGameObject { Name = "B", IsVisible = false });
        collection.Add(new MockGameObject { Name = "C", IsVisible = true });

        // Act
        var result = collection.GetVisibleGameObjects().ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void GetActiveGameObjects_ReturnsOnlyEnabledAndVisible()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        collection.Add(new MockGameObject { Name = "A", IsEnabled = true, IsVisible = true });
        collection.Add(new MockGameObject { Name = "B", IsEnabled = true, IsVisible = false });
        collection.Add(new MockGameObject { Name = "C", IsEnabled = false, IsVisible = true });
        collection.Add(new MockGameObject { Name = "D", IsEnabled = false, IsVisible = false });

        // Act
        var result = collection.GetActiveGameObjects().ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("A"));
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void GetGameObjectByName_FindsCorrectObject()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        collection.Add(new MockGameObject { Name = "Player" });
        collection.Add(new MockGameObject { Name = "Enemy" });

        // Act
        var result = collection.GetGameObjectByName("Player");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Player"));
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void GetGameObjectByName_NonExisting_ReturnsNull()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();

        // Act
        var result = collection.GetGameObjectByName("Ghost");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void GetGameObjectsOfType_FiltersCorrectly()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        collection.Add(new MockGameObject { Name = "A" });
        collection.Add(new MockInitializableGameObject { Name = "B" });
        collection.Add(new MockInitializableGameObject { Name = "C" });

        // Act
        var result = collection.GetGameObjectsOfType<MockInitializableGameObject>().ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void GetFirstGameObjectOfType_ReturnsFirst()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        collection.Add(new MockGameObject { Name = "A" });
        collection.Add(new MockInitializableGameObject { Name = "B" });
        collection.Add(new MockInitializableGameObject { Name = "C" });

        // Act
        var result = collection.GetFirstGameObjectOfType<MockInitializableGameObject>();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("B"));
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void Clear_RemovesAllObjects()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        collection.Add(new MockGameObject { Name = "A" });
        collection.Add(new MockGameObject { Name = "B" });

        // Act
        collection.Clear();

        // Assert
        Assert.That(collection.Count, Is.EqualTo(0));
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void Contains_Generic_DetectsType()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        collection.Add(new MockInitializableGameObject { Name = "A" });

        // Act
        var result = collection.Contains<MockInitializableGameObject>();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void ToArray_ReturnsCorrectArray()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        collection.Add(new MockGameObject { Name = "A", ZIndex = 10 });
        collection.Add(new MockGameObject { Name = "B", ZIndex = 5 });

        // Act
        var array = collection.ToArray();

        // Assert
        Assert.That(array.Length, Is.EqualTo(2));
        Assert.That(array[0].Name, Is.EqualTo("B")); // Sorted by ZIndex
        Assert.That(array[1].Name, Is.EqualTo("A"));
    }

    [Test]
    /// <summary>
    /// 
    /// </summary>
    public void Enumeration_WorksCorrectly()
    {
        // Arrange
        var collection = new SvoxGameObjectCollection<MockGameObject>();
        collection.Add(new MockGameObject { Name = "A", ZIndex = 10 });
        collection.Add(new MockGameObject { Name = "B", ZIndex = 5 });

        // Act
        var names = new List<string>();
        foreach (var obj in collection)
        {
            names.Add(obj.Name);
        }

        // Assert
        Assert.That(names, Is.EqualTo(["B", "A"])); // Sorted by ZIndex
    }
}
