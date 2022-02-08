using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class InputMovementSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireSingletonForUpdate<GameSettingsComponent>();
    }

    protected override void OnUpdate()
    {
        var gameSettings = GetSingleton<GameSettingsComponent>();
        var deltaTime = Time.DeltaTime;

        byte right, left, thrust, reverseThrust;
        right = left = thrust = reverseThrust = 0;

        float mouseX = 0;
        float mouseY = 0;

        if (Input.GetKey("d"))
        {
            right = 1;
        }
        if (Input.GetKey("a"))
        {
            left = 1;
        }
        if (Input.GetKey("w"))
        {
            thrust = 1;
        }
        if (Input.GetKey("s"))
        {
            reverseThrust = 1;
        }
        if (Input.GetMouseButton(1))
        {
            mouseX = Input.GetAxis("Mouse X");
            mouseY = Input.GetAxis("Mouse Y");
        }

        Entities
            .WithAll<PlayerTag>()
            .ForEach((Entity entity, int nativeThreadIndex, ref Rotation rotation, ref VelocityComponent velocity) =>
            {
                if (right == 1)
                {
                    velocity.Value += math.mul(rotation.Value, new float3(1, 0, 0)).xyz * gameSettings.playerForce * deltaTime;
                }
                if (left == 1)
                {
                    velocity.Value += math.mul(rotation.Value, new float3(-1, 0, 0)).xyz * gameSettings.playerForce * deltaTime;
                }
                if (thrust == 1)
                {
                    velocity.Value += math.mul(rotation.Value, new float3(0, 0, 1)).xyz * gameSettings.playerForce * deltaTime;
                }
                if (reverseThrust == 1)
                {
                    velocity.Value += math.mul(rotation.Value, new float3(0, 0, -1)).xyz * gameSettings.playerForce * deltaTime;
                }
                if (mouseX != 0 || mouseY != 0)
                {
                    float lookSpeedH = 2f;
                    float lookSpeedV = 2f;

                    Quaternion currentQuaternion = rotation.Value;
                    float yaw = currentQuaternion.eulerAngles.y;
                    float pitch = currentQuaternion.eulerAngles.x;

                    yaw += lookSpeedH * mouseX;
                    pitch -= lookSpeedV * mouseY;

                    Quaternion newQuaternion = Quaternion.identity;
                    newQuaternion.eulerAngles = new Vector3(pitch, yaw, 0);
                    rotation.Value = newQuaternion;
                }
            })
            .ScheduleParallel();
    }
}
