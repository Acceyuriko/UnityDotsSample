using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class MovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        Entities
            .ForEach((ref Translation position, in VelocityComponent velocity) =>
            {
                position.Value.xyz += velocity.Value * deltaTime;
            }).ScheduleParallel();
    }
}
