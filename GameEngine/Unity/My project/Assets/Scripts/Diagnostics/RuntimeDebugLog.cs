using UnityEngine;

namespace IT4s.Diagnostics
{
    /// <summary>
    /// Shared gate for ordinary informational Debug.Log output.
    /// Warnings, errors, and the explicit evaluation trace remain outside this gate.
    /// </summary>
    public static class RuntimeDebugLog
    {
        public static bool SuppressInformationalLogs { get; set; }

        public static void Log(string message)
        {
            if (SuppressInformationalLogs)
            {
                return;
            }

            Debug.Log(message);
        }
    }
}
