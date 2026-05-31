using System;

namespace AdminDb.Shared;

public interface IAdminDbShared
{
    const string Identity = nameof(IAdminDbShared);

    void RefreshNow();

    event Action? OnRefreshed;
}
