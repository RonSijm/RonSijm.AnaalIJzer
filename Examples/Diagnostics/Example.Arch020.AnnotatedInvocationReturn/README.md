# ARCH020: Annotated Invocation Return

`CanBeNullAttribute` is declared inside this project purely to demonstrate configuration-driven attribute matching. AnaalIJzer does not reference JetBrains.Annotations or any other attribute assembly. `<Invocation withAttribute="JetBrains.Annotations.CanBeNullAttribute" />` forbids returning that call unchanged, while `?? Pizza.Margherita` is a valid conversion to a usable result.

```cmd
dotnet build Examples\Diagnostics\Example.Arch020.AnnotatedInvocationReturn -c Release
```
