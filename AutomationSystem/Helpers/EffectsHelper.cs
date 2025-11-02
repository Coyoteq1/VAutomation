using Unity.Entities;
using ProjectM;
using Stunlock.Core;

namespace CrowbaneArena.Helpers
{
    public static class EffectsHelper
    {
        public static void PlaySound(Entity target, string soundEvent)
        {
            var em = VRisingCore.EntityManager;
            
            if (PrefabCollection.TryGetPrefabGUID(soundEvent, out var soundGuid))
            {
                var soundEntity = em.CreateEntity(
                    ComponentType.ReadWrite<FromCharacter>(),
                    ComponentType.ReadWrite<PlayAudioEvent>()
                );
                
                em.SetComponentData(soundEntity, new FromCharacter { Character = target });
                em.SetComponentData(soundEntity, new PlayAudioEvent { 
                    AudioEventGuid = soundGuid,
                    Volume = 1f,
                    Pitch = 1f
                });
            }
        }
        
        public static void SpawnParticles(Entity target, string effectPrefab)
        {
            var em = VRisingCore.EntityManager;
            
            if (PrefabCollection.TryGetPrefabGUID(effectPrefab, out var effectGuid))
            {
                var fxEntity = em.CreateEntity(
                    ComponentType.ReadWrite<FromCharacter>(),
                    ComponentType.ReadWrite<SpawnParticlesEffect>()
                );
                
                em.SetComponentData(fxEntity, new FromCharacter { Character = target });
                em.SetComponentData(fxEntity, new SpawnParticlesEffect { 
                    PrefabGUID = effectGuid,
                    Duration = 3f
                });
            }
        }
    }
}
