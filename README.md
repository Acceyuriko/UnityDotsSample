## Unity DOTS Sample

learn [dots-tutorial](https://dots-tutorial.moetsi.com/)

### 操作
空格创建角色，并发射子弹， wasd 控制移动，右键拖动控制视角。

### Multiplayer
#### Hosting a game
- 选择 `Host a game`，`ClientServerLaucnher` 则会启动 `ServerWorld`
- `ClientServerConnectionControl` 中会监听游戏端口。
- `GameServerBroadcasting` 启动一个 UdpClient，在 `BroadcastPort` 上向局域网内所有 `ReceivePort` 广播服务器消息。

#### Joinning a game
- `LocalGamesFinder` 启动一个 UdpClient，监听 `ReceivePort` 上的服务器消息，并将服务器消息显示在 `JoinGameScreen` 中。
- 用户选择一个服务器，并点击 `JoinGameScreen` 中的 `Join` 按钮，或通过 `ManulConnect` 手动指定一个服务器进行连接。
- `ClientServerLauncher` 会启动 `ClientWorld`。
- `ClientServerConnectionControl` 则会与服务器建立连接。

#### Leaving a game
- 点击 `Quit Game`。
- 当前是客户端
  - 在客户端的 NCE 中，加入 `NetworkStreamRequestDisconnect` Component。
  - 服务端遍历 `NetworkStreamDisconnected` 组件，将该 NCE 对应的客户端数据移除，如 PlayerEntity。
- 当前是服务端
  - 在所有 NCE 中，加入 `NetworkStreamRequestDisconnect` Component。
  - 客户端遍历 `NetworkStreamDisconnected` 组件，退出 `MainScene`，回到 `NavigationScene`


### 游戏启动

- `ClientServerConnectionHandler` 在 ServerWorld 与 ClientWorld 中根据编辑器里配置的 LaunchObject 生成初始化 Entity `InitializeServerComponent` 和 `InitializeClientComponent`。
- `ClientServerConnectionControl` 中， ServerWorld 执行初始化，监听了配置好的 port。ClientWorld 执行初始化，去尝试连接配置的 ip:port。

- `ServerSendGameSystem` 在收到客户端连接之后，向客户端发送游戏配置信息 `SendClientGameRpc`。
- `ClientLoadedGameSystem` 收到了 `SendClientGameRpc`，配置 `GameSettingsComponent`，并在 NCE 中加入 `NetworkStreamInGame` 等待接受 snapshots。然后向服务端发送 `SendServerGameLoadedRpc`。
- 服务端没有处理 `SendServerGameLoadedRpc` 的逻辑，但这个 Rpc 中本身在 `InvokeExecute` 中包含了会在服务端执行的代码，这个代码在服务端的 NCE 中加入 `NetworkStreamInGame`。至此连接完成。

### Asteroid 生成与销毁
Asteroid 完全由服务端控制，其 Ghost Mode 是 Interpolated。 
- `AsteroidSpawnSystem` 会在一个固定区域内生成一定数量的 Asteroid。
- `AsteroidsOutOfBoundsSystem` 检查每个 Asteroid 的位置，如果超出了边界，则添加 `DestroyTag`。
- `AsteroidsDestructionSystem` 检查每个 Asteroid 是否有 `DestroyTag`，如果有，则销毁它。

### Player 生成与销毁
Player 是 Predicted spawning for the client predicted player object。生成由服务端控制，交互则由两端一起处理。
- 客户端的 `InputSystem` 检查用户按下空格键，如果当前没有 Player，则向服务端发送 `PlayerSpawnRequestRpc`。
- 服务端的 `PlayerSpawnSystem` 收到 `PlayerSpawnRequestRpc`，创建一个新的 Player。添加上 `GhostOwnerComponent` 指定该客户端拥有它。添加上 `PlayerSpawnInProgressTag` 标记该 Player 已经在进行创建。添加上 `PlayerEntity` 标记该 Player 的 NCE。
- 服务端的 `PlayerCompleteSpawnSystem` 检查 `PlayerSpawnInProgressTag`。设置 NCE 的 `CommandTargetComponent` 为 Player 以存储 Command，并移除 `PlayerSpawnInProgressTag`。这里生成分为 Spawn 与 Complete 两步是为了防止 Player Entity 在生成时出现错误。
- 服务端的 Player Entity 生成之后，会包含在 snapshot 中发送给客户端。客户端的 `PlayerGhostSpawnClassificationSystem` 会在该 Entity 中添加 `GhostPlayerState` 以处理销毁，设置 NCE 的 `CommandTargetComponent` 为 Player，添加 Camera 修改用户摄像机。
- 客户端按下 p 时， `InputSystem` 会设置 `selfDestruct = 1`，并收集到 `PlayerCommand` 中，由 NetCode 同步给服务端。
- `InputResponseSpawnSystem` 同时运行在客户端与服务端。检查到 `PlayerCommand` 中 `selfDestruct == 1`，则给 Player Entity 添加 `DestroyTag`。
- 服务端的 `PlayerDestructionSystem` 检查 `PlayerTag` 与 `DestroyTag`，销毁 Player Entity。
- 客户端的 `PlayerGhostSpawnClassificationSystem` 检查 `PlayerTag` 与 `GhostPlayerState`，处理额外的销毁逻辑，设置 `CommandTargetComponent` 为 Entity.Null。

### Bullet 生成与销毁
Bullet 是 Predicted spawning for player spawned objects。 客户端和服务端会都会生成它，交互也由两端一起处理。
- 客户端的 `InputSystem` 检查用户按下空格键，且 Player Entity 存在，设置 `shoot == 1`，并收集到 `PlayerCommand` 中。
- `InputResponseSpawnSystem` 同时运行在客户端与服务端，它检查当前用户是否可以生成 Bullet, 如果可以，则生成 Bullet，并添加 `PredictedGhostSpawnRequestComponent` 使它出现在 `PredictedGhostSpawnList` 中。添加 `GhostOwnerComponent` 指定其所属的客户端。
- `BulletGhostSpawnClassificationSystem` 检查 `GhostSpawnQueueComponent` 中的 `GhostSpawnBuffer`，与 `PredictedGhostSpawnList` 中的 `PredictedGhostSpawn`，遍历两个 buffer。如果找到了相同的 entry，则设置 `ghost.PredictedSpawnEntity = spawn.Entity`，并移除 `PredictedGhostSpawn` 中的 entry。
- 客户端与服务端的物理系统一起处理碰撞，给 `Asteroid` 加上 `DestroyTag`，这里不详细讲。
- 服务端的 `BulletAgeSystem` 检查 `Bullet` 的存活时长，销毁掉 `Bullet`。

### Player 移动
- 客户端的 `InputSystem` 收集按键，组装成 `PlayerCommand`。
- `InputResponseMovementSystem` 同时运行在客户端与服务端，修改 Player 的 `VelocityComponent` 与 `Rotation` 处理移动。
  - 这里修改 `VelocityComponent` 而不是 `PhysicsVelocity`，是因为 `Unity.Physics` 目前不支持 `Prediction`。
- `MovementSystem` 同时运行在客户端与服务端，根据 `VelocityComponent` 处理 `Player` 的移动。