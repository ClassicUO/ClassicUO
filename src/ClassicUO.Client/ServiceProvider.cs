
using System;
using System.Collections.Generic;

namespace ClassicUO;


public sealed class ServiceProvider
{
    private readonly Dictionary<Type, IService> _services = new ();
    private readonly Dictionary<Type, List<Action<IEvent>>> _subscribedEvents = new ();


    public T Register<T>() where T : IService, new()
    {
        var service = new T ();
        _services.Add(typeof(T), service);
        return service;
    }

    public T Get<T>() where T : IService
    {
        _services.TryGetValue(typeof(T), out var service);
        return (T)service;
    }

    public void Raise<T>() where T : IEvent
        => Raise<T>(default);

    public void Raise<T>(T ev) where T : IEvent
    {
        if (_subscribedEvents.TryGetValue(typeof(T), out var eventList))
        {
            foreach (var fn in eventList)
                fn(ev);
        }
    }

    internal void LinkEvent<T>(Action<T> fn) where T : IEvent
    {
        if (!_subscribedEvents.TryGetValue(typeof(T), out var eventList))
        {
            eventList = new List<Action<IEvent>>();
            _subscribedEvents.Add(typeof(T), eventList);
        }

        eventList.Add(ev => fn((T)ev));
    }
}

class TestEventListener : EventListener
{
    public TestEventListener(ServiceProvider provider) : base(provider)
    {
        On<MouseEvent>(OnMouseEvent);
        On<PacketIncomingEvent>(OnPacketIncomingEvent);
    }

    void OnMouseEvent(MouseEvent ev)
    {

    }

    void OnPacketIncomingEvent(PacketIncomingEvent ev)
    {

    }
}

class TestService : Service
{
    public TestService()
    {

    }
}

public abstract class EventListener
{
    private readonly ServiceProvider _provider;

    protected EventListener(ServiceProvider provider)
        => _provider = provider;

    protected void On<T>(Action<T> fn) where T : IEvent
    {
        _provider.LinkEvent(fn);
    }
}


public interface IService
{
    public bool IsActive { get; set; }
}

public abstract class Service : IService
{
    public bool IsActive { get; set; }
}

public interface IEvent
{

}

public struct MouseEvent : IEvent { }

public readonly struct PacketIncomingEvent : IEvent { }

public readonly struct OnEngineTick : IEvent { }