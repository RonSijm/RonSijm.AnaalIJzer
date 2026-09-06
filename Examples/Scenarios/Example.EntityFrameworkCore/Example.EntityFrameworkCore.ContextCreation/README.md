# Entity Framework Core Context Creation

The factory may create and return `PizzaOrderingDbContext`. A repository may receive it through constructor injection. Application code may do neither, so its `new PizzaOrderingDbContext()` produces one `ARCH001` at the `New` site.

This is a site-filter example: the relationship itself is not enough; the configuration says where that relationship is permitted.
