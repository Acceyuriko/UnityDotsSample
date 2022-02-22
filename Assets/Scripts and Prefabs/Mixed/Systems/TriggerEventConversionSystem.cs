using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(StepPhysicsWorld))]
[UpdateBefore(typeof(EndFramePhysicsSystem))]
public class TriggerEventConversionSystem : SystemBase
{
    public JobHandle OutDependency => Dependency;

    private StepPhysicsWorld m_StepPhysicsWorld;
    private BuildPhysicsWorld m_BuildPhysicsWorld;
    private EndFramePhysicsSystem m_EndFramePhysicsSystem;
    private EntityQuery m_Query;

    private NativeList<StatefulTriggerEvent> m_PreviousFrameTriggerEvents;
    private NativeList<StatefulTriggerEvent> m_CurrentFrameTriggerEvents;

    public struct CollectTriggerEventsJob : ITriggerEventsJob
    {
        public NativeList<StatefulTriggerEvent> TriggerEvents;

        public void Execute(TriggerEvent triggerEvent)
        {
            TriggerEvents.Add(
                new StatefulTriggerEvent(
                    triggerEvent.EntityA,
                    triggerEvent.EntityB,
                    triggerEvent.BodyIndexA,
                    triggerEvent.BodyIndexB,
                    triggerEvent.ColliderKeyA,
                    triggerEvent.ColliderKeyB
                )
            );
        }
    }

    protected override void OnCreate()
    {
        m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_EndFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();
        m_Query = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(StatefulTriggerEvent) },
            None = new ComponentType[] { typeof(ExcludeFromTriggerEventConversion) }
        });

        m_PreviousFrameTriggerEvents = new NativeList<StatefulTriggerEvent>(Allocator.Persistent);
        m_CurrentFrameTriggerEvents = new NativeList<StatefulTriggerEvent>(Allocator.Persistent);
        RequireForUpdate(m_Query);
    }

    protected override void OnDestroy()
    {
        m_PreviousFrameTriggerEvents.Dispose();
        m_CurrentFrameTriggerEvents.Dispose();
    }

    protected override void OnUpdate()
    {
        Dependency = JobHandle.CombineDependencies(m_StepPhysicsWorld.FinalSimulationJobHandle, Dependency);

        SwapTriggerEventStates();
        var currentFrameTriggerEvents = m_CurrentFrameTriggerEvents;
        var previousFrameTriggerEvents = m_PreviousFrameTriggerEvents;
        var triggerEventBufferFromEntity = GetBufferFromEntity<StatefulTriggerEvent>();

        var collectTriggerEventsJob = new CollectTriggerEventsJob
        {
            TriggerEvents = currentFrameTriggerEvents
        };

        var collectJobHandle = collectTriggerEventsJob.Schedule(m_StepPhysicsWorld.Simulation, ref m_BuildPhysicsWorld.PhysicsWorld, Dependency);

        NativeHashMap<Entity, byte> entitiesWithBuffersMap = new NativeHashMap<Entity, byte>(0, Allocator.TempJob);

        var collectTriggerBuffersHandle = Entities
            .WithName("ClearTriggerEventDynamicBuffersJobParallel")
            .WithNone<ExcludeFromTriggerEventConversion>()
            .ForEach((Entity entity, ref DynamicBuffer<StatefulTriggerEvent> buffer) =>
            {
                buffer.Clear();
                entitiesWithBuffersMap.Add(entity, 0);
            })
            .Schedule(Dependency);

        Dependency = JobHandle.CombineDependencies(collectJobHandle, collectTriggerBuffersHandle);

        Dependency = Job
            .WithName("ConvertTriggerEventStreamToDynamicBufferJob")
            .WithDisposeOnCompletion(entitiesWithBuffersMap)
            .WithCode(() =>
            {
                currentFrameTriggerEvents.Sort();
                var triggerEventsWithStates = new NativeList<StatefulTriggerEvent>(currentFrameTriggerEvents.Length, Allocator.Temp);
                UpdateTriggerEventState(previousFrameTriggerEvents, currentFrameTriggerEvents, triggerEventsWithStates);
                AddTriggerEventsToDynamicBuffers(triggerEventsWithStates, ref triggerEventBufferFromEntity, entitiesWithBuffersMap);
            }).Schedule(Dependency);

        m_EndFramePhysicsSystem.AddInputDependency(Dependency);
    }

    private void SwapTriggerEventStates()
    {
        var temp = m_PreviousFrameTriggerEvents;
        m_PreviousFrameTriggerEvents = m_CurrentFrameTriggerEvents;
        m_CurrentFrameTriggerEvents = temp;
        m_CurrentFrameTriggerEvents.Clear();
    }

    public static void UpdateTriggerEventState(
        NativeList<StatefulTriggerEvent> previousFrameTriggerEvents,
        NativeList<StatefulTriggerEvent> currentFrameTriggerEvents,
        NativeList<StatefulTriggerEvent> resultList
    )
    {
        int i = 0;
        int j = 0;

        while (i < currentFrameTriggerEvents.Length && j < previousFrameTriggerEvents.Length)
        {
            var currentFrameTriggerEvent = currentFrameTriggerEvents[i];
            var previousFrameTriggerEvent = previousFrameTriggerEvents[j];

            var result = currentFrameTriggerEvent.CompareTo(previousFrameTriggerEvent);

            // Appears in previous, and current frame, mark it as Stay
            if (result == 0)
            {
                currentFrameTriggerEvent.State = StatefulTriggerEvent.EventOverlapState.Stay;
                resultList.Add(currentFrameTriggerEvent);
                i++;
                j++;
            }
            else if (result < 0)
            {
                // Appears in current, but not in previous, mark it as Enter
                currentFrameTriggerEvent.State = StatefulTriggerEvent.EventOverlapState.Enter;
                resultList.Add(currentFrameTriggerEvent);
                i++;
            }
            else
            {
                // Appears in previous, but not in current, mark it as Exit
                previousFrameTriggerEvent.State = StatefulTriggerEvent.EventOverlapState.Exit;
                resultList.Add(previousFrameTriggerEvent);
                j++;
            }
        }

        if (i == currentFrameTriggerEvents.Length)
        {
            while (j < previousFrameTriggerEvents.Length)
            {
                var triggerEvent = previousFrameTriggerEvents[j++];
                triggerEvent.State = StatefulTriggerEvent.EventOverlapState.Exit;
                resultList.Add(triggerEvent);
            }
        }
        else if (j == previousFrameTriggerEvents.Length)
        {
            while (i < currentFrameTriggerEvents.Length)
            {
                var triggerEvent = currentFrameTriggerEvents[i++];
                triggerEvent.State = StatefulTriggerEvent.EventOverlapState.Enter;
                resultList.Add(triggerEvent);
            }
        }
    }

    public static void AddTriggerEventsToDynamicBuffers(
        NativeList<StatefulTriggerEvent> triggerEventList,
        ref BufferFromEntity<StatefulTriggerEvent> bufferFromEntity,
        NativeHashMap<Entity, byte> entitiesWithTriggerBuffers
    )
    {
        for (int i = 0; i < triggerEventList.Length; i++)
        {
            var triggerEvent = triggerEventList[i];
            if (entitiesWithTriggerBuffers.ContainsKey(triggerEvent.Entities.EntityA))
            {
                bufferFromEntity[triggerEvent.Entities.EntityA].Add(triggerEvent);
            }
            if (entitiesWithTriggerBuffers.ContainsKey(triggerEvent.Entities.EntityB))
            {
                bufferFromEntity[triggerEvent.Entities.EntityB].Add(triggerEvent);
            }
        }
    }
}
