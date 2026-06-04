namespace Modernote.Protocol;

public sealed record ClientContext(string ClientId, ClientKind ClientKind)
{
    public static ClientContext LocalDesktop() =>
        new("local-desktop", ClientKind.Desktop);
}
