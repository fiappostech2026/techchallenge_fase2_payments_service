using FCG.Payments.Domain.Dto;
using FCG.Payments.Domain.Enums;
using FCG.Payments.Domain.Services;
using Xunit;

namespace FCG.Payments.Tests.Services;

public sealed class PaymentServiceTests
{
    [Fact]
    public void ProcessPayment_AlwaysApproveDelegate_ValidEvent_Returns_PaymentProcessedEvent_WithApprovedStatus()
    {
        var sut = new PaymentService(() => true);
        var evt = new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Price = 29.99m
        };

        var result = sut.ProcessPayment(evt);

        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Approved, result.Status);
        Assert.Equal(evt.OrderId, result.OrderId);
        Assert.Equal(evt.UserId, result.UserId);
        Assert.Equal(evt.GameId, result.GameId);
    }

    [Fact]
    public void ProcessPayment_AlwaysRejectDelegate_ValidEvent_Returns_PaymentProcessedEvent_WithRejectedStatus()
    {
        var sut = new PaymentService(() => false);
        var evt = new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Price = 29.99m
        };

        var result = sut.ProcessPayment(evt);

        Assert.Equal(PaymentStatus.Rejected, result.Status);
        Assert.Equal(evt.OrderId, result.OrderId);
        Assert.Equal(evt.UserId, result.UserId);
        Assert.Equal(evt.GameId, result.GameId);
    }

    [Fact]
    public void ProcessPayment_NullEvent_Throws_ArgumentNullException()
    {
        var sut = new PaymentService(() => true);

        Assert.Throws<ArgumentNullException>(() => sut.ProcessPayment(null!));
    }

    [Fact]
    public void ProcessPayment_PriceAtDecimalMaxValue_DoesNotThrow()
    {
        var sut = new PaymentService(() => true);
        var evt = new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Price = decimal.MaxValue
        };

        var result = sut.ProcessPayment(evt);

        Assert.NotNull(result);
    }

    [Fact]
    public void ProcessPayment_PriceAtDecimalMinValue_ServiceDoesNotThrow()
    {
        var sut = new PaymentService(() => true);
        var evt = new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Price = decimal.MinValue
        };

        var result = sut.ProcessPayment(evt);

        Assert.NotNull(result);
    }
}
