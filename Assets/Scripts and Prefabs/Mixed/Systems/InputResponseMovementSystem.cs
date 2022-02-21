using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

[UpdateInWorld(UpdateInWorld.TargetWorld.ClientAndServer)]
public class InputResponseMovementSystem : SystemBase
{
    private GhostPredictionSystemGroup m_PredictionGroup;

    protected override void OnCreate()
    {
        m_PredictionGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();
    }

    protected override void OnUpdate()
    {
        var currentTick = m_PredictionGroup.PredictingTick;
        var deltaTime = m_PredictionGroup.Time.DeltaTime;

        var playerForce = GetSingleton<GameSettingsComponent>().playerForce;
        var inputFromEntity = GetBufferFromEntity<PlayerCommand>(true);

        Entities
            .WithReadOnly(inputFromEntity)
            .WithAll<PlayerTag, PlayerCommand>()
            .ForEach((Entity entity, int nativeThreadIndex, ref Rotation rotation, ref VelocityComponent velocity, in GhostOwnerComponent ghostOwner, in PredictedGhostComponent prediction) =>
            {
                if (!GhostPredictionSystemGroup.ShouldPredict(currentTick, prediction))
                {
                    return;
                }

                var input = inputFromEntity[entity];

                PlayerCommand inputData;
                if (!input.GetDataAtTick(currentTick, out inputData)) inputData.shoot = 0;

                if (inputData.right == 1) velocity.Linear += math.mul(rotation.Value, new float3(1, 0, 0).xyz * playerForce * deltaTime);
                if (inputData.left == 1) velocity.Linear += math.mul(rotation.Value, new float3(-1, 0, 0).xyz * playerForce * deltaTime);
                if (inputData.thrust == 1) velocity.Linear += math.mul(rotation.Value, new float3(0, 0, 1).xyz * playerForce * deltaTime);
                if (inputData.reverseThrust == 1) velocity.Linear += math.mul(rotation.Value, new float3(0, 0, -1).xyz * playerForce * deltaTime);

                if (inputData.mouseX != 0 || inputData.mouseY != 0)
                {
                    float lookSpeedH = 2f;
                    float lookSpeedV = 2f;

                    Quaternion currentQuaternion = rotation.Value;
                    float yaw = currentQuaternion.eulerAngles.y;
                    float pitch = currentQuaternion.eulerAngles.x;

                    yaw += lookSpeedH * inputData.mouseX;
                    pitch -= lookSpeedV * inputData.mouseY;
                    Quaternion newQuaternion = Quaternion.identity;
                    newQuaternion.eulerAngles = new Vector3(pitch, yaw, 0);
                    rotation.Value = newQuaternion;
                }
            })
            .ScheduleParallel();
    }
}