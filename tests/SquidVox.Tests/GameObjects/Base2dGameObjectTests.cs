using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NUnit.Framework;
using SquidVox.Core.GameObjects;

namespace SquidVox.Tests.GameObjects;

/// <summary>
/// Unit tests for Base2dGameObject class.
/// </summary>
[TestFixture]
public class Base2dGameObjectTests
{
    private class TestGameObject : Base2dGameObject
    {
        public int OnUpdateCallCount { get; private set; }
        public int OnRenderCallCount { get; private set; }

        protected override void OnUpdate(GameTime gameTime)
        {
            OnUpdateCallCount++;
        }

        protected override void OnRender(SpriteBatch spriteBatch)
        {
            OnRenderCallCount++;
        }
    }

    [Test]
    public void Constructor_InitializesDefaultValues()
    {
        // Arrange & Act
        var gameObject = new TestGameObject();

        // Assert
        Assert.That(gameObject.Name, Is.EqualTo("Unnamed GameObject"));
        Assert.That(gameObject.ZIndex, Is.EqualTo(0));
        Assert.That(gameObject.IsEnabled, Is.True);
        Assert.That(gameObject.IsVisible, Is.True);
        Assert.That(gameObject.Position, Is.EqualTo(Vector2.Zero));
        Assert.That(gameObject.Scale, Is.EqualTo(Vector2.One));
        Assert.That(gameObject.Rotation, Is.EqualTo(0f));
        Assert.That(gameObject.Parent, Is.Null);
        Assert.That(gameObject.HasFocus, Is.False);
        Assert.That(gameObject.Children, Is.Empty);
    }

    [Test]
    public void GetAbsolutePosition_WithNoParent_ReturnsLocalPosition()
    {
        // Arrange
        var gameObject = new TestGameObject
        {
            Position = new Vector2(10, 20)
        };

        // Act
        var absolutePosition = gameObject.GetAbsolutePosition();

        // Assert
        Assert.That(absolutePosition, Is.EqualTo(new Vector2(10, 20)));
    }

    [Test]
    public void GetAbsolutePosition_WithParent_ReturnsParentPlusLocalPosition()
    {
        // Arrange
        var parent = new TestGameObject
        {
            Position = new Vector2(100, 200)
        };

        var child = new TestGameObject
        {
            Position = new Vector2(10, 20)
        };

        parent.AddChild(child);

        // Act
        var absolutePosition = child.GetAbsolutePosition();

        // Assert
        Assert.That(absolutePosition, Is.EqualTo(new Vector2(110, 220)));
    }

    [Test]
    public void GetAbsolutePosition_WithMultipleLevelsOfParents_CalculatesCorrectly()
    {
        // Arrange
        var grandParent = new TestGameObject
        {
            Position = new Vector2(100, 100)
        };

        var parent = new TestGameObject
        {
            Position = new Vector2(50, 50)
        };

        var child = new TestGameObject
        {
            Position = new Vector2(10, 10)
        };

        grandParent.AddChild(parent);
        parent.AddChild(child);

        // Act
        var absolutePosition = child.GetAbsolutePosition();

        // Assert
        // 100 + 50 + 10 = 160, 100 + 50 + 10 = 160
        Assert.That(absolutePosition, Is.EqualTo(new Vector2(160, 160)));
    }

    [Test]
    public void GetAbsoluteScale_WithNoParent_ReturnsLocalScale()
    {
        // Arrange
        var gameObject = new TestGameObject
        {
            Scale = new Vector2(2, 3)
        };

        // Act
        var absoluteScale = gameObject.GetAbsoluteScale();

        // Assert
        Assert.That(absoluteScale, Is.EqualTo(new Vector2(2, 3)));
    }

    [Test]
    public void GetAbsoluteScale_WithParent_ReturnsMultipliedScale()
    {
        // Arrange
        var parent = new TestGameObject
        {
            Scale = new Vector2(2, 2)
        };

        var child = new TestGameObject
        {
            Scale = new Vector2(0.5f, 0.5f)
        };

        parent.AddChild(child);

        // Act
        var absoluteScale = child.GetAbsoluteScale();

        // Assert
        Assert.That(absoluteScale, Is.EqualTo(new Vector2(1, 1)));
    }

    [Test]
    public void GetAbsoluteRotation_WithNoParent_ReturnsLocalRotation()
    {
        // Arrange
        var gameObject = new TestGameObject
        {
            Rotation = MathF.PI / 4 // 45 degrees
        };

        // Act
        var absoluteRotation = gameObject.GetAbsoluteRotation();

        // Assert
        Assert.That(absoluteRotation, Is.EqualTo(MathF.PI / 4));
    }

    [Test]
    public void GetAbsoluteRotation_WithParent_ReturnsSumOfRotations()
    {
        // Arrange
        var parent = new TestGameObject
        {
            Rotation = MathF.PI / 4 // 45 degrees
        };

        var child = new TestGameObject
        {
            Rotation = MathF.PI / 4 // 45 degrees
        };

        parent.AddChild(child);

        // Act
        var absoluteRotation = child.GetAbsoluteRotation();

        // Assert
        Assert.That(absoluteRotation, Is.EqualTo(MathF.PI / 2).Within(0.0001f)); // 90 degrees
    }

    [Test]
    public void AddChild_AddsChildAndSetsParent()
    {
        // Arrange
        var parent = new TestGameObject();
        var child = new TestGameObject();

        // Act
        parent.AddChild(child);

        // Assert
        Assert.That(parent.Children, Contains.Item(child));
        Assert.That(child.Parent, Is.EqualTo(parent));
    }

    [Test]
    public void AddChild_DoesNotAddDuplicateChild()
    {
        // Arrange
        var parent = new TestGameObject();
        var child = new TestGameObject();

        // Act
        parent.AddChild(child);
        parent.AddChild(child); // Try to add again

        // Assert
        Assert.That(parent.Children.Count(), Is.EqualTo(1));
    }

    [Test]
    public void RemoveChild_RemovesChildAndClearsParent()
    {
        // Arrange
        var parent = new TestGameObject();
        var child = new TestGameObject();
        parent.AddChild(child);

        // Act
        parent.RemoveChild(child);

        // Assert
        Assert.That(parent.Children, Does.Not.Contain(child));
        Assert.That(child.Parent, Is.Null);
    }

    [Test]
    public void Update_WhenDisabled_DoesNotCallOnUpdate()
    {
        // Arrange
        var gameObject = new TestGameObject { IsEnabled = false };
        var gameTime = new GameTime();

        // Act
        gameObject.Update(gameTime);

        // Assert
        Assert.That(gameObject.OnUpdateCallCount, Is.EqualTo(0));
    }

    [Test]
    public void Update_WhenEnabled_CallsOnUpdate()
    {
        // Arrange
        var gameObject = new TestGameObject { IsEnabled = true };
        var gameTime = new GameTime();

        // Act
        gameObject.Update(gameTime);

        // Assert
        Assert.That(gameObject.OnUpdateCallCount, Is.EqualTo(1));
    }

    [Test]
    public void Update_UpdatesEnabledChildren()
    {
        // Arrange
        var parent = new TestGameObject();
        var child1 = new TestGameObject { IsEnabled = true };
        var child2 = new TestGameObject { IsEnabled = false };

        parent.AddChild(child1);
        parent.AddChild(child2);

        var gameTime = new GameTime();

        // Act
        parent.Update(gameTime);

        // Assert
        Assert.That(child1.OnUpdateCallCount, Is.EqualTo(1));
        Assert.That(child2.OnUpdateCallCount, Is.EqualTo(0));
    }

    [Test]
    public void Render_WhenInvisible_DoesNotCallOnRender()
    {
        // Arrange
        var gameObject = new TestGameObject { IsVisible = false };

        // Act
        gameObject.Render(null!);

        // Assert
        Assert.That(gameObject.OnRenderCallCount, Is.EqualTo(0));
    }

    [Test]
    public void Render_WhenVisible_CallsOnRender()
    {
        // Arrange
        var gameObject = new TestGameObject { IsVisible = true };

        // Act
        gameObject.Render(null!);

        // Assert
        Assert.That(gameObject.OnRenderCallCount, Is.EqualTo(1));
    }

    [Test]
    public void Render_RendersVisibleChildren()
    {
        // Arrange
        var parent = new TestGameObject();
        var child1 = new TestGameObject { IsVisible = true };
        var child2 = new TestGameObject { IsVisible = false };

        parent.AddChild(child1);
        parent.AddChild(child2);

        // Act
        parent.Render(null!);

        // Assert
        Assert.That(child1.OnRenderCallCount, Is.EqualTo(1));
        Assert.That(child2.OnRenderCallCount, Is.EqualTo(0));
    }
}
