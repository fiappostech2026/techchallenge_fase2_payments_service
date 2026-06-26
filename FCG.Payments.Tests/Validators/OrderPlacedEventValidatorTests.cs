using FCG.Payments.Domain.Dto;
using FCG.Payments.Domain.Validators;
using Xunit;

namespace FCG.Payments.Tests.Validators;

public sealed class OrderPlacedEventValidatorTests
{
    private readonly OrderPlacedEventValidator _sut = new();

    [Fact]
    public void OrderPlacedEventValidator_AllFieldsValid_ValidationPasses()
    {
        var evt = new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Price = 10.00m
        };

        var result = _sut.Validate(evt);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void OrderPlacedEventValidator_EmptyOrderId_Fails_WithOrderIdError()
    {
        var evt = new OrderPlacedEvent
        {
            OrderId = Guid.Empty,
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Price = 1m
        };

        var result = _sut.Validate(evt);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(OrderPlacedEvent.OrderId));
    }

    [Fact]
    public void OrderPlacedEventValidator_EmptyUserId_Fails_WithUserIdError()
    {
        var evt = new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.Empty,
            GameId = Guid.NewGuid(),
            Price = 1m
        };

        var result = _sut.Validate(evt);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(OrderPlacedEvent.UserId));
    }

    [Fact]
    public void OrderPlacedEventValidator_EmptyGameId_Fails_WithGameIdError()
    {
        var evt = new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.Empty,
            Price = 1m
        };

        var result = _sut.Validate(evt);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(OrderPlacedEvent.GameId));
    }

    [Fact]
    public void OrderPlacedEventValidator_ZeroPrice_Fails_WithPriceError()
    {
        var evt = new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Price = 0m
        };

        var result = _sut.Validate(evt);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(OrderPlacedEvent.Price));
    }

    [Fact]
    public void OrderPlacedEventValidator_NegativePrice_Fails_WithPriceError()
    {
        var evt = new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Price = -1.00m
        };

        var result = _sut.Validate(evt);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(OrderPlacedEvent.Price));
    }

    [Fact]
    public void OrderPlacedEventValidator_AllGuidsEmpty_FailsOnAllThreeGuidFields()
    {
        var evt = new OrderPlacedEvent
        {
            OrderId = Guid.Empty,
            UserId = Guid.Empty,
            GameId = Guid.Empty,
            Price = 1m
        };

        var result = _sut.Validate(evt);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 3);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(OrderPlacedEvent.OrderId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(OrderPlacedEvent.UserId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(OrderPlacedEvent.GameId));
    }
}
