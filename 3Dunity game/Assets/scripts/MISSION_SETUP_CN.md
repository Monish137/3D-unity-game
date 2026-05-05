# 第一章任务系统接入说明

这个项目现在已经有以下任务脚本：

- `MissionManager`
- `MissionTrigger`
- `MissionCollectible`
- `MissionInteractable`

## 一、先把任务列表自动填进去

1. 在场景里创建一个空物体，命名为 `MissionManager`
2. 给它挂上 `MissionManager.cs`
3. 在 Inspector 右上角点击组件菜单
4. 选择 `Load Chapter One Missions`

这样会自动填入第一章的 10 个任务：

1. 去见艾克特
2. 领取新剑
3. 收集猎物
4. 回去复命
5. 查看告示
6. 询问梅林
7. 通知艾克特
8. 武装村民
9. 保卫村庄
10. 战后交谈

## 二、把 UI 绑到任务管理器

如果你的 Canvas 里已经有 3 个 `Text`：

- 任务标题
- 任务描述
- 任务进度

把它们分别拖到：

- `Mission Title Text`
- `Mission Description Text`
- `Mission Progress Text`

如果暂时没有 UI，也可以先留空，任务流程依然能跑。

## 三、主角必须满足的条件

玩家角色必须：

- Tag 是 `Player`
- 身上有 Collider
- 推荐同时有 Rigidbody 或 CharacterController

否则触发器可能不会生效。

## 四、每个任务该放什么物体

### 1. 去见艾克特 `go_to_ector`

在艾克特门前放一个空物体：

- 添加 `BoxCollider`
- 勾选 `Is Trigger`
- 挂 `MissionTrigger`
- `Mission Id` 填 `go_to_ector`

### 2. 领取新剑 `pick_up_sword`

在剑物体上：

- 添加 Collider
- 勾选 `Is Trigger`
- 挂 `MissionCollectible`
- `Mission Id` 填 `pick_up_sword`
- `Amount` 填 `1`

### 3. 收集猎物 `hunt_for_food`

放 3 个猎物物体：

- 每个都挂 `MissionCollectible`
- `Mission Id` 都填 `hunt_for_food`
- `Amount` 都填 `1`

### 4. 回去复命 `return_to_ector`

在艾克特身边再放一个触发区：

- 挂 `MissionTrigger`
- `Mission Id` 填 `return_to_ector`

### 5. 查看告示 `read_notice_board`

在布告栏前放一个触发区：

- 挂 `MissionTrigger`
- `Mission Id` 填 `read_notice_board`

### 6. 询问梅林 `talk_to_merlin`

在梅林 NPC 身上或旁边放一个触发区：

- 添加 Collider
- 勾选 `Is Trigger`
- 挂 `MissionInteractable`
- `Mission Id` 填 `talk_to_merlin`
- `Require Interaction Key` 保持勾选
- 按键默认是 `E`

### 7. 通知艾克特 `warn_ector`

在艾克特 NPC 身上或旁边放一个触发区：

- 挂 `MissionInteractable`
- `Mission Id` 填 `warn_ector`

### 8. 武装村民 `arm_villagers`

最简单做法有两种，先任选一种：

方案 A：4 个武器收集物

- 放 4 把武器
- 每个挂 `MissionCollectible`
- `Mission Id` 填 `arm_villagers`

方案 B：4 个村民交付点

- 在 4 个村民身边各放一个触发区
- 每个挂 `MissionInteractable`
- `Mission Id` 填 `arm_villagers`

### 9. 保卫村庄 `defend_village`

当前最简单先做成到达战斗区域：

- 在村口放一个大触发区
- 挂 `MissionTrigger`
- `Mission Id` 填 `defend_village`

后面如果你要做真正的“击败敌人”任务，我们再单独加敌人击杀统计。

### 10. 战后交谈 `talk_after_battle`

在梅林或艾克特旁边放一个触发区：

- 挂 `MissionInteractable`
- `Mission Id` 填 `talk_after_battle`

## 五、现在这套脚本的使用原则

- `MissionTrigger`：到达某地点就完成
- `MissionCollectible`：碰到可收集物就加进度
- `MissionInteractable`：走近后按 `E` 才完成

## 六、最容易出错的地方

- `Mission Id` 必须和任务列表里的 `id` 完全一致
- 玩家 Tag 必须是 `Player`
- Collider 要勾 `Is Trigger`
- 场景里必须有且只有一个主要的 `MissionManager`

## 七、建议你在 Unity 里的接入顺序

1. 先把 `MissionManager` 放进场景
2. 执行 `Load Chapter One Missions`
3. 先接前 3 个任务
4. 运行测试是否能从 `go_to_ector -> pick_up_sword -> hunt_for_food`
5. 确认没问题后，再继续接后面的任务

## 八、下一步最值得做的增强

如果你希望任务系统更像正式游戏，下一步最值得加的是：

- 对话 UI 提示，比如“按 E 对话”
- 敌人击杀计数任务
- 任务完成提示动画
- 小地图任务标记
