十字路口餐厅 Crossroads Diner
Unity 可玩原型与素材替换说明

实现依据
《Crossroads_Diner_Prototype_Design_v0_1.docx》提供经营规则、物种、配方与默认数值。
《Crossroads_Diner_Interaction_Revision_v0_2.docx》优先决定操作、镜头状态及信息显示。
采用 v0.2 建议默认值：S 后退、允许跨镜头持物、转场释放取消、双原料拖入锅自动开煮。

一、开始游戏
Unity 项目：E:/GitHubWorkspace/UUGameJam/UUGameJam
场景：Assets/Crossroads/Scenes/CrossroadsDiner.unity
打开场景，按 Unity Play，然后点击 Start Shift。
独立 Windows 版本：CrossroadsDiner-Windows/CrossroadsDiner.exe。
整个 Windows 文件夹必须一起保留，不能只复制 exe。

二、操作
W：在前台台面推近检查顾客。订单仅在招待近景显示。
S：从招待近景退回台面。
A / D：分别向左／向右转到另一工作区。近景会先退后再转身。
鼠标左键按住：拿起物品；拖到合法目标后松开提交。
持物时可以继续按 W、S、A、D，鼠标仍要保持按住。
右键：取消拖拽。Esc：暂停／继续，并取消未提交拖拽。
暂停菜单的 - / + 调整音量。失焦时，独立版本自动暂停。
暂停菜单新增 HOW TO PLAY：查看完整按键、操作流程、怪物弱点与配方速查。
查看教程时仍保持暂停；教程内按 Esc 或 BACK TO PAUSE 返回暂停菜单，再选择 RESUME SHIFT 继续。
Tab 没有切换功能；菜谱卡仅供查阅，点击不会开煮。

一次完整循环
1. W 检查需求与弱点，S 回台面。
2. 按住克制道具，W 靠近，拖到对应顾客并松开。
3. A 或 D 转后厨，拖捕获物到处理台，3 秒产出两份原料。
4. 分别拖两份兼容原料到锅中，第二份投放成功后自动烹煮 4 秒。
5. 成品自动进入共享餐食位；回前台台面，按住成品并 W 推近。
6. 拖给点了该菜的顾客，立即收钱。

三、默认内容
吸血鬼 Vampire：大蒜 Garlic 克制，产出 Blood x2。
狼人 Werewolf：银锁链 Silver chains 克制，产出 Meat x2。
幽灵 Ghost：封魂盐 Spirit salt 克制，产出 Ecto x2。

Blood Stew：Blood + Meat，12 元。
Spectral Stew：Ecto + Meat，14 元。
Haunted Soup：Blood + Ecto，16 元。

300 秒一局；4 个等候位、2 个捕获位、1 个处理台、1 口锅、2 个餐食位。
开局每种原料 2 份；固定三位开局顾客，第四位空缺。
来客间隔按 10 / 8 / 6 秒，耐心按 50 / 45 / 40 秒分阶段变化。
物种与菜品分别使用洗牌袋，不会按库存自动补缺少的答案。

四、素材替换
所有新增内容在 Assets/Crossroads 中，不依赖外部网络服务。
初版采用可编辑的矢量占位美术与短提示音，目的是立即试玩并方便正式美术替换。

Characters / Items
Assets/Crossroads/Data/Monsters：物种、弱点、产出与角色展示。
Assets/Crossroads/Data/Tools：克制道具名称与展示。
Assets/Crossroads/Data/Ingredients：原料名称与展示。
Assets/Crossroads/Data/Recipes：菜品名称、原料组合、售价、烹饪时间与展示。
每个资产都具有 icon、visualPrefab、placeholder、tint 等展示字段。

替换静态图片：导入 PNG，Texture Type 设为 Sprite (2D and UI)，把图拖入 icon；清空 visualPrefab。
替换动画角色：使用 RectTransform 根节点的 UI Prefab，可含 Image、Animator 和子节点，拖入 visualPrefab。
优先级：visualPrefab > icon > 矢量占位。两个字段都为空时仍可运行。
不要修改稳定 id 来做美术改名；显示名称使用 displayName。
现有占位 Prefab 在 Visuals/Characters 与 Visuals/Items，可独立编辑。

Backgrounds / Stations / UI / Audio
Assets/Crossroads/Data/DinerTheme.asset：
  frontBackground / kitchenBackground：两个工作区背景图。
  frontDecorationPrefab / kitchenDecorationPrefab：可选附加 UI 装饰。
  processorIcon / potIcon：处理台与炖锅图标。
  panelSprite / slotSprite：面板与槽位皮肤，建议使用带 Border 的九宫格 Sprite。
  bodyFont / titleFont：字体。
  ink / cream / gold / mint / danger：主界面配色。
  ambience、arrival、capture、invalid、processed、cooked、paid、walkout：环境音乐与反馈音。
  defaultVolume：首次运行的默认音量。

替换主题、公开菜谱文字或布局容量后：停止 Play，选择菜单
Crossroads > Rebuild presentation from theme。
此操作重新生成默认布局并保存场景与展示 Prefab；自行调整过的布局建议先做 Prefab Variant 或备份。
仅替换物种／菜品的 icon、visualPrefab 或音频，重新进入 Play 即可生效。
完整界面 Prefab：Assets/Crossroads/Prefabs/DinerPresentation.prefab。
场景连接该 Prefab。可在 Prefab Mode 修改尺寸、位置、背景、字体等，保留 DinerHitArea 与控制器引用即可。

五、数值与扩展
Assets/Crossroads/Data/DinerConfig.asset 控制容量、时长、开局库存、开局顾客、随机种子与阶段曲线。
默认布局按三种原料／物种／配方设计；扩充数量后应重新生成布局并检查文字和槽位尺寸。
配方使用两个原料引用，可顺序互换；同一个原料对不能定义两道不同菜。
先修改配置，不需要改业务脚本。Crossroads > Validate catalog 会检查引用与重复配置。

六、代码职责
Runtime/DinerSession.cs：权威状态、计时、来客、事务、库存守恒、拖拽预留、视图状态。
Runtime/DinerGame.cs：Input System 输入、帧更新顺序、音效、暂停与菜单命令。
Runtime/DinerPresentation.cs：显示与镜头动画，不拥有业务时钟。
Runtime/DinerHitArea.cs：鼠标命中区，向模型提交目标。
Runtime/DinerVisual.cs / DinerArtGraphic.cs：可替换视觉层与占位绘制。
Editor/DinerInstaller.cs：初始资源与场景生成、主题重建。
Tests：Edit Mode 规则测试与 Play Mode 鼠标／键盘端到端测试。

七、边界规则
错误道具、错误菜或无效释放均不扣资源。
抓捕立即取消该顾客订单，不直接产出原料，也不自动切后厨。
拖拽中的菜仍占原槽；暂停、无效释放与取消不会制造副本。
第一份入锅原料被预留，可拖回同类原料容器；第二份兼容原料才提交消耗并开煮。
满盘时成品留锅保温，释放餐食位后自动移出；拖起未交付的菜不算腾位。
过期顾客被新顾客补位后，旧拖拽不会作用于新顾客。
隐藏工作区仍推进时间；暂停冻结所有业务时间。
关门先停止交易；关门未服务与耐心流失分开统计。
重开清空旧局的库存、预留、拖拽、顾客、设备与统计。

八、美术与许可
本次新增矢量图形和短提示音由项目代码生成，无第三方下载美术。
界面与教程字体使用用户提供的 Alice_in_Wonderland_3.ttf（Alice in Wonderland，作者 Marco T.L.）。原始说明 Shhhst!.txt 随字体保存在 Assets/Crossroads/Visuals/Fonts，并随可运行包保存在 Licenses 中。附带说明限制非商业用途；字体文件及说明保持原样。DinerTheme 的 bodyFont / titleFont 可继续替换。
保留现有 Unity 工程及包依赖；后续替换外部美术、字体或音乐时，将作者与许可记录在项目素材说明中。

九、验证范围
已执行自动规则测试与真实输入层测试，并检查三个主要视图。
这不等同于外部玩家对经营平衡与趣味性的验证；价格、耐心和来客节奏保留为可调数据。
详细构建与验证结果见同目录的 CrossroadsDiner_验证记录.txt。
