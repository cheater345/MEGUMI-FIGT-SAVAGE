using UnityEngine;
using SteelTempest.Core.Di;
using SteelTempest.Core.Events;
using SteelTempest.Save;
using SteelTempest.Economy;
using SteelTempest.Combat;

namespace SteelTempest.Core.Bootstrap
{
    /// <summary>
    /// Bootstraps the game: registers core services, wires the event bus
    /// and initialises save/economy/combat singletons before gameplay starts.
    /// Place on a persistent GameObject in the Boot scene.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private bool dontDestroyOnLoad = true;

        private void Awake()
        {
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            var locator = ServiceLocator.Instance;

            // Core singletons registered as instances so systems share one instance.
            locator
                .RegisterInstance(EventBus.Instance)
                .RegisterInstance(new SaveManager())
                .RegisterInstance(new CurrencyManager());

            locator.Resolve<SaveManager>().Initialize();
            locator.Resolve<CurrencyManager>().Initialize();

            Debug.Log("[SteelTempest] Bootstrap complete.");
        }

        private void OnApplicationPause(bool paused)
        {
            // Persist progress whenever the app backgrounds or resumes.
            if (paused)
            {
                ServiceLocator.Instance.Resolve<SaveManager>().Save();
            }
        }
    }
}