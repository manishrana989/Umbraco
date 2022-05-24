using DCHMediaPicker.Core.Models;
using System.Collections.Generic;

namespace DCHMediaPicker.Core.Services.Interfaces
{
    public interface IExpiryService
    {
        List<ExpiryEmailData> SendExpiryReminders();
    }
}