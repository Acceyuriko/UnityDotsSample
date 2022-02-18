using AOT;
using UnityEngine;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Entities;
using Unity.Burst;

[BurstCompile]
public struct SendServerGameLoadedRpc : IComponentData, IRpcCommandSerializer<SendServerGameLoadedRpc>
{
    public void Serialize(ref DataStreamWriter writer, in RpcSerializerState state, in SendServerGameLoadedRpc data) { }

    public void Deserialize(ref DataStreamReader reader, in RpcDeserializerState state, ref SendServerGameLoadedRpc data) { }

    [BurstCompile]
    [MonoPInvokeCallback(typeof(RpcExecutor.ExecuteDelegate))]
    private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
    {
        var rpcData = default(SendServerGameLoadedRpc);
        rpcData.Deserialize(ref parameters.Reader, parameters.DeserializerState, ref rpcData);

        parameters.CommandBuffer.AddComponent(parameters.JobIndex, parameters.Connection, new PlayerSpawningStateComponent());
        parameters.CommandBuffer.AddComponent(parameters.JobIndex, parameters.Connection, default(NetworkStreamInGame));
        parameters.CommandBuffer.AddComponent(parameters.JobIndex, parameters.Connection, default(GhostConnectionPosition));

        Debug.Log("Server acted on confirmed game load");
    }

    static PortableFunctionPointer<RpcExecutor.ExecuteDelegate> InvokeExecuteFunctionPointer =
        new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);

    public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
    {
        return InvokeExecuteFunctionPointer;
    }
}

class SendServerGameLoadedRpcCommandRequestSystem : RpcCommandRequestSystem<SendServerGameLoadedRpc, SendServerGameLoadedRpc>
{
    [BurstCompile]
    protected struct SendRpc: IJobEntityBatch
    {
        public SendRpcData data;
        public void Execute(ArchetypeChunk chunk, int orderIndex)
        {
            data.Execute(chunk, orderIndex);
        }
    }

    protected override void OnUpdate()
    {
        var sendJob = new SendRpc{data = InitJobData()};
        ScheduleJobData(sendJob);
    }
}