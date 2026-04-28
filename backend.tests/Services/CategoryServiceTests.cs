using backend.Dtos;
using backend.Helpers;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class CategoryServiceTests
    {
        private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();

        private CategoryService CreateService()
        {
            return new CategoryService(_categoryRepositoryMock.Object);
        }

        private static Category CreateCategory(
            int id = 1,
            string name = "Power Tools",
            string slug = "power-tools",
            bool isActive = true)
        {
            return new Category
            {
                Id = id,
                Name = name,
                Slug = slug,
                Icon = "tool-icon",
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCategory_WhenCategoryExistsAndIsActive()
        {
            // Arrange
            var service = CreateService();

            var category = CreateCategory();

            _categoryRepositoryMock
                .Setup(r => r.GetByIdAsync(category.Id))
                .ReturnsAsync(category);

            _categoryRepositoryMock
                .Setup(r => r.GetItemCountAsync(category.Id))
                .ReturnsAsync(5);

            // Act
            var result = await service.GetByIdAsync(category.Id);

            // Assert
            result.Id.Should().Be(category.Id);
            result.Name.Should().Be(category.Name);
            result.Slug.Should().Be(category.Slug);
            result.Icon.Should().Be(category.Icon);
            result.IsActive.Should().BeTrue();
            result.ItemCount.Should().Be(5);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrow_WhenCategoryDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _categoryRepositoryMock
                .Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync((Category?)null);

            // Act
            Func<Task> act = async () => await service.GetByIdAsync(99);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Category 99 not found.");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrow_WhenCategoryIsInactiveAndUserIsNotAdmin()
        {
            // Arrange
            var service = CreateService();

            var category = CreateCategory(isActive: false);

            _categoryRepositoryMock
                .Setup(r => r.GetByIdAsync(category.Id))
                .ReturnsAsync(category);

            // Act
            Func<Task> act = async () => await service.GetByIdAsync(category.Id, isAdmin: false);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Category {category.Id} not found.");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnInactiveCategory_WhenUserIsAdmin()
        {
            // Arrange
            var service = CreateService();

            var category = CreateCategory(isActive: false);

            _categoryRepositoryMock
                .Setup(r => r.GetByIdAsync(category.Id))
                .ReturnsAsync(category);

            _categoryRepositoryMock
                .Setup(r => r.GetItemCountAsync(category.Id))
                .ReturnsAsync(2);

            // Act
            var result = await service.GetByIdAsync(category.Id, isAdmin: true);

            // Assert
            result.Id.Should().Be(category.Id);
            result.IsActive.Should().BeFalse();
            result.ItemCount.Should().Be(2);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateCategory_WhenNameIsUnique()
        {
            // Arrange
            var service = CreateService();

            var dto = new CreateCategoryDto
            {
                Name = " Power Tools ",
                Icon = " tool-icon "
            };

            _categoryRepositoryMock
                .Setup(r => r.ExistsByNameAsync("Power Tools"))
                .ReturnsAsync(false);

            _categoryRepositoryMock
                .Setup(r => r.ExistsBySlugAsync("power-tools"))
                .ReturnsAsync(false);

            _categoryRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Category>()))
                .Callback<Category>(category =>
                {
                    category.Id = 1;
                })
                .Returns(Task.CompletedTask);

            _categoryRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            result.Id.Should().Be(1);
            result.Name.Should().Be("Power Tools");
            result.Slug.Should().Be("power-tools");
            result.Icon.Should().Be("tool-icon");
            result.IsActive.Should().BeTrue();
            result.ItemCount.Should().Be(0);

            _categoryRepositoryMock.Verify(r => r.AddAsync(It.Is<Category>(c =>
                c.Name == "Power Tools" &&
                c.Slug == "power-tools" &&
                c.Icon == "tool-icon" &&
                c.IsActive
            )), Times.Once);

            _categoryRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenNameAlreadyExists()
        {
            // Arrange
            var service = CreateService();

            var dto = new CreateCategoryDto
            {
                Name = "Power Tools",
                Icon = "tool-icon"
            };

            _categoryRepositoryMock
                .Setup(r => r.ExistsByNameAsync("Power Tools"))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await service.CreateAsync(dto);

            // Assert
            await act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("A category named 'Power Tools' already exists.");

            _categoryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Never);
            _categoryRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateCategory_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var category = CreateCategory(
                id: 1,
                name: "Old Name",
                slug: "old-name",
                isActive: true);

            var dto = new UpdateCategoryDto
            {
                Name = "New Name",
                Icon = "new-icon",
                IsActive = false
            };

            _categoryRepositoryMock
                .Setup(r => r.GetByIdAsync(category.Id))
                .ReturnsAsync(category);

            _categoryRepositoryMock
                .Setup(r => r.ExistsByNameAsync("New Name"))
                .ReturnsAsync(false);

            _categoryRepositoryMock
                .Setup(r => r.ExistsBySlugAsync("new-name"))
                .ReturnsAsync(false);

            _categoryRepositoryMock
                .Setup(r => r.GetItemCountAsync(category.Id))
                .ReturnsAsync(3);

            _categoryRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.UpdateAsync(category.Id, dto);

            // Assert
            category.Name.Should().Be("New Name");
            category.Slug.Should().Be("new-name");
            category.Icon.Should().Be("new-icon");
            category.IsActive.Should().BeFalse();

            result.Name.Should().Be("New Name");
            result.Slug.Should().Be("new-name");
            result.ItemCount.Should().Be(3);

            _categoryRepositoryMock.Verify(r => r.Update(category), Times.Once);
            _categoryRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ToggleActiveAsync_ShouldToggleCategoryActiveState()
        {
            // Arrange
            var service = CreateService();

            var category = CreateCategory(isActive: true);

            _categoryRepositoryMock
                .Setup(r => r.GetByIdAsync(category.Id))
                .ReturnsAsync(category);

            _categoryRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.ToggleActiveAsync(category.Id);

            // Assert
            category.IsActive.Should().BeFalse();

            _categoryRepositoryMock.Verify(r => r.Update(category), Times.Once);
            _categoryRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteCategory_WhenCategoryHasNoItems()
        {
            // Arrange
            var service = CreateService();

            var category = CreateCategory();

            _categoryRepositoryMock
                .Setup(r => r.GetByIdAsync(category.Id))
                .ReturnsAsync(category);

            _categoryRepositoryMock
                .Setup(r => r.GetItemCountAsync(category.Id))
                .ReturnsAsync(0);

            _categoryRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.DeleteAsync(category.Id);

            // Assert
            _categoryRepositoryMock.Verify(r => r.Delete(category), Times.Once);
            _categoryRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrow_WhenCategoryHasItems()
        {
            // Arrange
            var service = CreateService();

            var category = CreateCategory();

            _categoryRepositoryMock
                .Setup(r => r.GetByIdAsync(category.Id))
                .ReturnsAsync(category);

            _categoryRepositoryMock
                .Setup(r => r.GetItemCountAsync(category.Id))
                .ReturnsAsync(5);

            // Act
            Func<Task> act = async () => await service.DeleteAsync(category.Id);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Cannot delete a category with items.");

            _categoryRepositoryMock.Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
            _categoryRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }
    }
}