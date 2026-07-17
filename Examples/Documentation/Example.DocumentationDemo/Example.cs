// ReSharper disable All - Justification: Example File
using System;

namespace Example.DocumentationDemo;

public interface ILogger
{
    void Info(string message);
}

public interface IUseCase;

public abstract class ApplicationServiceBase;

public sealed class ApiControllerAttribute : Attribute;

[ApiController]
public sealed class OrderEndpoint(OrderService service, ILogger logger)
{
    public OrderProjection GetOrder()
    {
        logger.Info("Returning one projected order.");
        return service.GetOrder();
    }
}

public sealed class OrderService(OrderRepository repository, ILogger logger, TelemetryClient telemetry) : ApplicationServiceBase, IUseCase
{
    public OrderProjection GetOrder()
    {
        logger.Info("Preparing the query without keeping the query surface.");
        telemetry.Track("order-query-started");
        return repository.QueryOrders().ForCurrentCustomer().Project();
    }

    private sealed class OperationNote;
}

public sealed class HealthDiagnostics(OrderService service, ILogger logger)
{
    public void Check()
    {
        logger.Info(service.GetOrder().OrderId.ToString());
    }
}

public sealed class OrderQuery
{
    public OrderQuery ForCurrentCustomer()
    {
        return this;
    }

    public OrderProjection Project()
    {
        return new OrderProjection(42);
    }
}

public sealed record OrderProjection(int OrderId);

public sealed class OrderRepository
{
    public OrderQuery QueryOrders()
    {
        return new OrderQuery();
    }
}

public sealed class TelemetryClient
{
    public void Track(string name) { }
}

public sealed class InvoiceEntryPoint(InvoiceWorkflow workflow)
{
    public void Pay()
    {
        workflow.Run();
    }
}

public sealed class InvoiceWorkflow(PaymentGateway gateway)
{
    public void Run()
    {
        gateway.Charge();
    }
}

public sealed class PaymentGateway
{
    public void Charge() { }
}

public sealed class LegacyAdapter;