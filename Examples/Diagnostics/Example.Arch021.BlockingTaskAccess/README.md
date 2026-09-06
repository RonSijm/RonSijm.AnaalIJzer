# ARCH021: Blocking Task Access

`PizzaKitchen` may `await` preparation, but `Task.Wait()` and `Task<T>.Result` are configured as forbidden selected operations. They demonstrate that one policy can use different site filters: `Method` for `Wait`, and `Local` for `Result`.

```cmd
dotnet build Examples\Diagnostics\Example.Arch021.BlockingTaskAccess -c Release
```
