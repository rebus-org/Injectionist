# Injectionist

The Injectionist is a dependency injection tool that can be used to e.g. configure something.

It came to life when Rebus needed a flexible built-in IoC container-like thing that supported decorators. 
Rebus uses it to register stuff throughout the `Configure.With(...)` spell, finally calling 
`injectionist.Get<IBus>()` at the end to resolve the bus and have all of its dependencies injected.


### How to use it?

First, create an `Injectionist` instance:

    var injectionist = new Injectionist();

Then, register a type-to-func mapping in it:

    injectionist.Register<ISomething>(c => new ImplementationOfSomething());

where `ImplementationOfSomething` is obsiously an implementation of `ISomething`.

If a class has one or more dependencies and needs to have stuff injected, get that stuff from the `IResolutionContext` passed to the func:

    injectionist.Register(c => new HomeController(c.Get<ISomething>()));

Now, let's pretend that I want to decorate `ImplementationOfSomething` with another implementation of `ISomething` - do this:

    injectioninst.Register<ISomething>(c => new SomethingDecorator(c.Get<ISomething>()), isDecorator: true);

The Injectionist will always resolve decorators first, in the order that they're registered in, recursively moving
closer to the primary implementation for each nested call to `c.Get<ISomething>()`.

When the time comes to actually create an instance of something, just `Get` it:

    var instance = injectionist.Get<HomeController>();

It's pretty simple, but it's still kind of neat.