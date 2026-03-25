using System;

namespace DRAUM.Modules.Combat.Animation.Contracts
{
    /// <summary>
    /// Контракт: источник состояния анимации для камеры.
    /// </summary>
    public interface ICameraAnimationStateProvider
    {
        bool TryGetFloat(string parameterName, out float value);

        bool TryGetBool(string parameterName, out bool value);

        bool TryGetInt(string parameterName, out int value);

        event Action<string> TriggerFired;

        string GetCurrentCameraAnimationLayerName();
    }
}
