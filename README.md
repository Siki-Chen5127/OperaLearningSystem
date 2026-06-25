# 畅音雅韵：戏曲艺术学习平台项目介绍

## 一、项目定位

**畅音雅韵** 是一个面向中国戏曲艺术学习、欣赏、互动与内容共创的 Web 平台。项目以“戏曲知识学习 + 沉浸式文化体验 + 社区交流 + 后台内容治理”为主线，把剧种、剧目、名家、课程、古建戏台、AI 角色扮演、戏词心灯、雅集社区等功能组织在同一个 ASP.NET Core MVC 应用中。

平台并不是单纯的信息展示站，而是一个带用户体系、内容审核、学习记录、互动反馈和 AI 辅助学习的综合系统。前台强调国风视觉与沉浸式交互，后台强调内容维护、用户权限和数据看板。

本说明基于项目源码、Razor 页面、配置、实体模型、服务层、控制器、前端脚本和数据库导出文件整理；未逐个展开第三方静态库、图标包、字体、图片、音频、视频、3D 模型等多媒体或供应商资产。

## 二、解决方案结构

项目采用DDD架构，Visual Studio 解决方案文件为 `OperaLearningSystem.sln`。

```text
OperaLearningSystem-main/
├─ Core/                       核心领域层：实体、DTO、服务接口
├─ Application/                应用服务层：业务服务实现
├─ Infrastructure/             基础设施层：EF Core DbContext、迁移、补丁脚本
├─ OperaLearningSystem.Web/    Web 层：MVC 控制器、Razor 视图、后台 Area、静态资源
└─ OperaLearningSystemDB_Data.sql  数据库结构与示例数据导出
```

各层职责：

- `Core`：定义领域实体，如剧种、剧目、课程、社区帖子、用户、AI 角色、测验题等；同时定义分页 DTO 和服务接口。
- `Application`：实现剧种、剧目、名家、课程、社区、评论、点赞、收藏、用户、戏台知识等业务服务。
- `Infrastructure`：提供 `OperaDbContext`，配置实体关系、索引和删除策略；包含 EF Core 迁移与课程测验结构补丁。
- `OperaLearningSystem.Web`：提供前台页面、后台管理、API、SignalR Hub、AI 服务、静态脚本和样式。

## 三、技术栈

### 后端

- .NET 8 / ASP.NET Core MVC
- Razor Views + Areas
- ASP.NET Core Identity，用户主键为 `int`
- Entity Framework Core 8
- SQL Server 作为当前启动配置数据库
- EF Core SQLite 包也被引用，项目中同时存在 `Opera.db`，但 `Program.cs` 当前使用 SQL Server 连接串
- SignalR，用于梨园心灯实时推送
- MemoryCache，用于剧种/剧目/课程/词云等缓存场景
- Hosted Service，用于定期刷新社区词云缓存
- Semantic Kernel 1.68 + OpenAI 兼容接口，用于 DeepSeek 聊天与 AI 命题

### 前端

- Razor + Bootstrap 布局体系
- jQuery / jQuery Validation
- Bootstrap Icons / Font Awesome 图标
- ECharts，用于后台图表
- Select2、SweetAlert2、DataTables 等增强组件
- `model-viewer` 展示 3D 模型
- 原生 JavaScript、Canvas、WebGL，用于首页沉浸式交互和梦境转场
- AOS 动画、Lottie、tsparticles 等视觉增强资源

### 数据与资源

- `OperaLearningSystemDB_Data.sql` 导出数据库结构和示例数据
- `wwwroot/data/theaters.json` 提供首页戏台卡片数据
- `wwwroot/images`、`wwwroot/videos`、`wwwroot/audios`、`wwwroot/models` 提供多媒体与 3D 资源
- `wwwroot/uploads/community` 存储社区上传媒体

## 四、核心功能模块

### 1. 沉浸式首页与畅音阁导览

首页由 `HomeController` 加载推荐剧目和随机戏词，并通过 `Views/Home/Index.cshtml` 与 `wwwroot/js/pages/Home/home-entrance.js` 实现沉浸体验。

主要能力：

- 登录用户可进入沉浸式“畅音阁”导览。
- 入口包含“擦门”“叩门”等 Canvas 交互。
- 支持三层戏楼场景切换。
- 支持 3D 屋顶模型查看，模型有流畅、标准、极致三档，并带加载进度和缓存策略。
- 支持古建戏台分区卷轴弹窗，从 `/api/opera/stage-regions` 动态读取戏台区域和条目。
- 支持高清建筑图片画廊。
- 首页动态读取 `theaters.json` 展示故宫畅音阁、颐和园德和园、承德避暑山庄清音阁等戏台知识。

### 2. 剧种方志

对应前台控制器 `CategoryController`，后台控制器 `Areas/Admin/Controllers/CategoryController.cs`。

主要能力：

- 前台分页浏览剧种。
- 按关键词搜索剧种。
- 查看剧种详情，包括简介、历史、关联剧目、名家、课程入口。
- 后台可新增、编辑、删除剧种并上传封面。
- 普通管理员提交内容后进入待审核，超级管理员可直接发布。

核心实体：`Category`

重要字段：

- `Id`
- `Name`
- `ParentId`
- `ImageUrl`
- `Description`
- `History`
- `SubmitterId`
- `AuditStatus`
- 导航属性：`Plays`、`Courses`、`Masters`、`Posts`

### 3. 剧目典藏

对应前台控制器 `PlayController`，后台控制器 `Areas/Admin/Controllers/PlayController.cs`。

主要能力：

- 前台分页浏览剧目。
- 按关键词和剧种筛选剧目。
- 查看剧目详情、剧情简介、视频链接、封面图、关联名家。
- 用户可点赞、收藏、评论。
- 后台可维护剧目资料、封面、所属剧种、关联名家。
- 剧目提交支持审核状态。

核心实体：`Play`

重要字段：

- `Id`
- `Title`
- `Synopsis`
- `VideoUrl`
- `CategoryId`
- `ImageUrl`
- `SubmitterId`
- `AuditStatus`
- 导航属性：`Category`、`Comments`、`Favorites`、`Likes`、`PlayMasters`

剧目与名家是多对多关系，通过 `PlayMaster` 连接。

### 4. 名家风采

对应前台控制器 `MasterController`，后台控制器 `Areas/Admin/Controllers/MasterController.cs`。

主要能力：

- 前台分页浏览戏曲名家。
- 按关键词和剧种筛选名家。
- 查看名家详情、简介、评分、关联剧目。
- 用户可点赞、收藏。
- 后台可新增、编辑、删除名家并上传头像或封面。

核心实体：`Master`

重要字段：

- `Id`
- `Name`
- `Introduction`
- `ImageUrl`
- `Rating`
- `CategoryId`
- `SubmitterId`
- `AuditStatus`
- 导航属性：`Category`、`Favorites`、`Likes`、`PlayMasters`

### 5. 梨园私塾课程

对应前台控制器 `CourseController`，后台控制器 `Areas/Admin/Controllers/CourseController.cs`。

主要能力：

- 前台分页浏览课程。
- 按关键词和剧种筛选课程。
- 查看课程详情、课程简介、视频地址或 Bilibili 嵌入内容。
- 进入课程学习页 `Study`。
- 用户可点赞、收藏、评论。
- 后台可维护课程资料、课程封面、精选状态。
- 后台可为课程生成、编辑、删除专属测验题。

核心实体：`Course`

重要字段：

- `Id`
- `Name`
- `Description`
- `VideoUrl`
- `BilibiliEmbedHtml`
- `CategoryId`
- `IsFeatured`
- `ImageUrl`
- `SubmitterId`
- `AuditStatus`
- 导航属性：`Category`、`Comments`、`Favorites`、`Likes`、`QuizQuestions`

### 6. 课程测验与学习画像

测验 API 位于 `QuizApiController`，后台 AI 命题服务位于 `CourseQuizAiService`。

主要能力：

- `/api/quiz/next`：获取全站通用练习题。
- `/api/quiz/answer`：提交通用练习答案，更新用户能力估计。
- `/api/quiz/course/start`：开始课程专属固定题量考卷。
- `/api/quiz/course/answer`：提交课程考卷答案，推进当前题目，结束后记录成绩。
- 后台课程管理可调用 AI 根据课程名称和简介生成 3-8 道单选题。
- 答题过程会维护 `UserLearningProfile`，包括能力估计、连对、连错。
- 完成课程考卷后写入 `UserCourseQuizAttempt`。
- 连续答对或全对可获得 `UserBadge`。

核心实体：

- `QuizQuestion`：题库
- `UserLearningProfile`：学习画像
- `UserCourseQuizSession`：进行中的课程考卷会话
- `UserCourseQuizAttempt`：已完成的课程考卷记录
- `UserBadge`：徽章

`CourseQuizSchemaPatcher` 说明项目曾使用 `EnsureCreated`，因此启动时执行幂等 SQL 补齐课程测验、楼中楼评论和评论投票相关结构。

### 7. 梨园雅集社区

社区前台由 `CommunityController` 和 `CommunityFeedApiController` 共同提供。传统帖子详情页仍走 MVC；新版信息流、词云、互动和媒体上传走 API。

社区分为三类：

- `PostKind = 0`：戏词雅集
- `PostKind = 1`：戏台打卡
- `PostKind = 2`：百宝阁作品分享

主要能力：

- 推荐信息流 `/api/community-feed/recommended`
- 支持智能排序、时间排序、热度排序、地区排序
- 登录用户根据个人兴趣 `Hobbies` 做偏好加权
- 词云接口 `/api/community-feed/word-cloud`
- 词云筛选同时匹配帖子和评论
- 支持发雅集帖、打卡帖、作品帖
- 支持上传图片/视频媒体到 `wwwroot/uploads/community/yyyyMM`
- 支持帖子评论、快速评论
- 支持多种互动反应：赞、花、彩、藏、转
- 帖子收藏可在个人中心查看

核心实体：`CommunityPost`

重要字段：

- `Id`
- `Title`
- `Content`
- `CategoryId`
- `AuthorId`
- `CreatedTime`
- `PostKind`
- `TopicTags`
- `MediaUrls`
- `RegionLabel`
- 导航属性：`Author`、`Category`、`Comments`、`PostLikes`

### 8. 评论、楼中楼与投票

评论能力由 `CommentController`、`CommentService` 和共享 Razor 片段 `_CommentPartial`、`_CommentThreadScripts` 支持。

主要能力：

- 剧目、课程、社区帖子均可评论。
- 评论支持父评论 `ParentCommentId`，可形成楼中楼回复。
- `CommentVote` 记录用户对评论的赞/踩。
- `CommentVoteStatsHelper` 用于加载评论投票统计和当前用户投票状态。

核心实体：

- `Comment`
- `CommentVote`

### 9. 点赞与收藏

点赞和收藏由 `InteractionController`、`LikeService`、`FavoriteService` 提供。

支持对象：

- 剧目
- 课程
- 名家
- 社区帖子
- 评论

`Like` 使用多个可空外键表达不同对象类型，并额外用 `ReactionKind` 区分社区信息流的不同反应。代码中特别处理了“单一外键 + 其他为空”的匹配方式，避免 SQL 中 `NULL` 条件误匹配无关数据。

### 10. AI 助手“小梨”

`AiController` 提供 `/api/Ai/chat`。

主要能力：

- 使用 Semantic Kernel 构建聊天内核。
- 使用 DeepSeek 兼容 OpenAI Chat Completion 接口。
- 注入 `OperaNavigationPlugin`。
- 小梨可根据用户意图导航到首页、剧目、名家、剧种、课程、社区、戏词、个人中心等页面。
- 可查询数据库中的剧目和名家，并在匹配单条时返回跳转命令。
- 可介绍网站功能亮点。

插件函数：

- `NavigateToPage`
- `SearchPlays`
- `SearchMasters`
- `GetWebsiteHighlights`

### 11. AI 角色扮演“梨园梦境”

`AiRoleplayController` 和 `Views/AiRoleplay` 负责沉浸式角色扮演。

主要能力：

- 角色梳妆页 `Dresser` 可匿名访问，用于选择角色。
- 正式对话页要求登录，因为需要保存记忆。
- 每个 AI 角色配置独立头像、背景、系统提示词、开场白、启用状态和排序。
- 用户与某角色的聊天记录保存到 `AiChatMessages`。
- 调用 DeepSeek Chat API 时只取最近 10 条历史，控制上下文长度。
- 用户可清空某角色历史。
- `DreamPersonaSummary` 会根据对话中的关键词沉淀用户偏好，如昆曲、京剧、牡丹亭、脸谱、戏台等。

核心实体：

- `AiCharacter`
- `AiChatMessage`
- `User.DreamPersonaSummary`

### 12. 梨园心灯与戏词

`QuoteBoardController` 与 `QuoteHub` 负责“梨园心灯”。

主要能力：

- 用户发布或展示戏词/心灯内容。
- 使用 SignalR Hub `/quoteHub` 推送实时更新。
- `OperaQuote` 保存用户发布的名句或心灯内容。
- `OperaLyric` 保存戏词、解读、来源剧目。

相关实体：

- `OperaQuote`
- `OperaLyric`

### 13. 用户体系与个人中心

`AccountController` 负责认证、资料、个人中心与密码流程。

主要能力：

- 注册、登录、注销。
- 忘记密码、重置密码、修改密码。
- 完善/编辑个人资料。
- 上传头像到 `wwwroot/images/avatars`。
- 选择籍贯、省份、性别、爱好剧种。
- 个人中心展示：
  - 用户资料
  - 收藏的剧目/课程/名家
  - 点赞的剧目/课程/名家
  - 课程测验历史
  - 社区帖子收藏
- 新用户注册后跳转完善资料。

核心实体：`User`

扩展字段：

- `AvatarUrl`
- `CreatedAt`
- `Nickname`
- `BirthDate`
- `Gender`
- `Province`
- `Bio`
- `Hobbies`
- `DreamPersonaSummary`

### 14. 管理员申请与审核大厅

`AdminApplicationController` 负责用户提交管理员申请，`AuditController` 负责超级管理员审核。

主要能力：

- 登录用户可递交“拜帖”申请成为管理员。
- 超级管理员可审批或驳回管理员申请。
- 超级管理员可审核普通管理员提交的剧目、名家、剧种、课程。
- 审核状态：
  - `0`：待审核
  - `1`：通过
  - `2`：驳回

核心实体：`AdminApplication`

重要字段：

- `Id`
- `UserId`
- `Reason`
- `Status`
- `RejectReason`
- `CreatedAt`
- `ProcessedAt`

### 15. 后台管理系统

后台位于 `OperaLearningSystem.Web/Areas/Admin`，所有后台控制器继承 `BaseAdminController`，要求角色为 `Admin` 或 `SuperAdmin`。

后台模块：

- 首页数据驾驶舱
- 剧种管理
- 剧目管理
- 名家管理
- 古建戏台库
- 私塾课程管理
- 课程专属题库管理
- AI 角色管理
- 雅集帖子管控
- 用户管理
- 审核大厅入口

后台首页统计：

- 剧目总量
- 用户总量
- AI 对话总量
- 社区帖子总量
- 待审核申请数
- 最新用户
- 最新帖子
- 剧种剧目占比图
- 近 7 天 AI 梦境沉浸频次
- 系统生态雷达图
- 用户互动行为环形图

## 五、数据结构概览

### 主要实体关系

```text
User
├─ CommunityPosts
├─ Comments
├─ Favorites
├─ Likes
├─ AdminApplications
├─ SubmittedPlays / SubmittedMasters / SubmittedCourses / SubmittedCategories
└─ UserLearningProfile

Category
├─ Plays
├─ Courses
├─ Masters
└─ CommunityPosts

Play
├─ Category
├─ Comments
├─ Favorites
├─ Likes
└─ PlayMasters ── Master

Course
├─ Category
├─ Comments
├─ Favorites
├─ Likes
└─ QuizQuestions

CommunityPost
├─ Author(User)
├─ Category
├─ Comments
└─ PostLikes(Like)

AiCharacter
└─ AiChatMessages

OperaStageRegion
└─ OperaStages
```

### 数据表清单

数据库导出文件中包含以下业务表和 Identity 表：

- `AdminApplications`
- `AiCharacters`
- `AiChatMessages`
- `AspNetRoles`
- `AspNetUsers`
- `AspNetUserRoles`
- `AspNetRoleClaims`
- `AspNetUserClaims`
- `AspNetUserLogins`
- `AspNetUserTokens`
- `Categories`
- `Comments`
- `CommentVotes`
- `CommunityPosts`
- `Courses`
- `Favorites`
- `Likes`
- `Masters`
- `OperaLyrics`
- `OperaQuotes`
- `OperaStageRegions`
- `OperaStages`
- `PlayMasters`
- `Plays`
- `QuizQuestions`
- `UserBadges`
- `UserCourseQuizAttempts`
- `UserCourseQuizSessions`
- `UserLearningProfiles`
- `__EFMigrationsHistory`

### 示例数据规模

`OperaLearningSystemDB_Data.sql` 中可见的核心示例数据包括：

- 剧种 `Categories`：9 条
- 剧目 `Plays`：17 条
- 名家 `Masters`：21 条
- 课程 `Courses`：7 条
- 戏词 `OperaLyrics`：5 条
- 心灯/名句 `OperaQuotes`：8 条
- AI 角色 `AiCharacters`：7 条
- 题库 `QuizQuestions`：17 条
- 戏台分区 `OperaStageRegions`：8 条
- 戏台条目 `OperaStages`：18 条

## 六、接口与路由概览

### 前台 MVC 页面

- `/Home/Index`：首页与沉浸式入口
- `/Home/About`：关于页面
- `/Category/Index`、`/Category/Details/{id}`：剧种方志
- `/Play/Index`、`/Play/Details/{id}`：剧目典藏
- `/Master/Index`、`/Master/Details/{id}`：名家风采
- `/Course/Index`、`/Course/Details/{id}`、`/Course/Study/{id}`：课程与学习页
- `/Community/Index`：戏词雅集
- `/Community/Checkin`：戏台打卡
- `/Community/Works`：百宝阁
- `/Community/Details/{id}`：帖子详情
- `/QuoteBoard/Index`：梨园心灯
- `/AiRoleplay/Dresser`：AI 角色选择
- `/AiRoleplay/Index?characterId=...`：AI 角色扮演
- `/Account/Login`、`/Account/Register`、`/Account/UserCenter`：账号与个人中心
- `/AdminApplication/Apply`：管理员申请
- `/Audit/Index`：超级管理员审核大厅

### API

- `POST /api/Ai/chat`：小梨 AI 助手
- `GET /api/opera/stage-regions`：戏台分区和戏台条目
- `GET /api/opera/categories`：已审核剧种
- `GET /api/opera/categories/{id}`：剧种详情
- `GET /api/opera/categories/{id}/plays`：某剧种剧目
- `GET /api/opera/masters`：名家列表
- `GET /api/opera/masters/{id}`：名家详情
- `GET /api/opera/plays/{id}/masters`：某剧目关联名家
- `GET /api/community-feed/recommended`：社区推荐信息流
- `POST /api/community-feed/lyrics-post`：发布雅集帖
- `POST /api/community-feed/checkin`：发布打卡帖
- `POST /api/community-feed/work`：发布作品帖
- `POST /api/community-feed/react/{postId}`：社区互动反应
- `POST /api/community-feed/upload-media`：社区媒体上传
- `GET /api/community-feed/comments/{postId}`：获取帖子评论
- `POST /api/community-feed/comments/{postId}`：发布帖子评论
- `GET /api/community-feed/word-cloud`：社区词云
- `GET /api/quiz/next`：通用练习下一题
- `POST /api/quiz/answer`：提交通用练习答案
- `POST /api/quiz/course/start`：开始课程考卷
- `POST /api/quiz/course/answer`：提交课程考卷答案
- `GET /api/dashboard/categoryplaycounts`：后台图表数据

### SignalR

- `/quoteHub`：梨园心灯实时消息 Hub

## 七、后台权限模型

平台使用 ASP.NET Core Identity 角色：

- `Admin`：可进入后台管理区，维护内容、课程、AI 角色、用户等。
- `SuperAdmin`：拥有超级管理员权限，可进入审核大厅，审批管理员申请和内容投稿。

启动时 `Program.cs` 会确保 `Admin` 和 `SuperAdmin` 角色存在，并创建默认超级管理员账号。文档不记录默认密码和密钥等敏感信息，实际部署时应改为安全配置。

## 八、前端体验设计

项目的前端不是普通后台模板，而是大量围绕戏曲审美做了定制：

- 全局水墨/国风配色、印章 Logo、古风导航和覆盖式导航面板。
- 首页“畅音阁”以多场景方式呈现，结合 Canvas、视频、3D 模型和弹窗。
- 剧种、剧目、名家、课程分别有独立页面样式。
- 课程页面被包装成“传习私塾”。
- 社区信息流使用雅集、打卡、百宝阁三个文化化名称。
- AI 角色扮演页面使用“梳妆入戏”和沉浸式背景。
- 后台虽然是管理系统，但也保留国风视觉，并配有数据图表。

主要自写前端脚本：

- `wwwroot/js/pages/Home/home-entrance.js`：首页沉浸入口、三层戏楼、3D 模型、图集、戏台数据加载。
- `wwwroot/js/community-yaji-feed.js`：雅集/打卡/百宝阁共用信息流、词云、排序、搜索、评论、互动。
- `wwwroot/js/yaji-compose-modal.js`：社区发帖弹窗控制。
- `wwwroot/js/dream-vortex.js`：AI 梦境 WebGL/Canvas 转场动画。

## 九、数据库与持久化

当前 `Program.cs` 使用：

```csharp
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("OperaLearningSystem.Infrastructure"))
```

默认连接串在 `appsettings.json` 中配置。项目引用了 EF Core SQLite 包，Web 项目下也存在 `Opera.db`，但当前启动逻辑以 SQL Server 为准。

启动时会执行：

- `Database.EnsureCreatedAsync()`
- `CourseQuizSchemaPatcher.EnsureAsync(context)`
- 角色与超级管理员种子初始化
- 若题库为空，则写入一组戏曲常识、古建戏台和跨界知识题

## 十、内容治理与审核流程

平台对用户共创内容有一套基础治理流程：

- 用户可申请成为管理员。
- 超级管理员审批后给用户添加 `Admin` 角色。
- 普通管理员新增或修改剧种、剧目、名家、课程时，内容可进入待审核状态。
- 超级管理员在审核大厅统一处理待审内容。
- 通过后 `AuditStatus = 1`，前台可展示。
- 驳回后 `AuditStatus = 2`。

## 十一、安全与配置注意事项

源码中存在 AI 配置项 `AiSettings:ApiKey`，说明项目需要外部大模型服务。实际部署建议：

- 不要在仓库中提交真实 API Key。
- 使用环境变量、用户机密、部署平台 Secret 或安全配置中心。
- 默认管理员密码应在首次部署后立即修改。
- 社区上传接口已限制部分图片/视频扩展名和 30 MB 请求大小，但生产环境还应增加 MIME 校验、病毒扫描、对象存储隔离和访问控制。
- `ServeUnknownFileTypes = true` 为模型资源提供了便利，但生产环境应谨慎限制可访问目录和扩展名。
- 邮件服务当前为控制台模拟输出，如需真实找回密码邮件，需要接入 SMTP 或第三方邮件服务。

## 十二、项目亮点

- 以戏曲学习为中心，不只展示资料，还提供课程学习、测验、学习画像和个人记录。
- 内容结构完整，涵盖剧种、剧目、名家、课程、戏台、戏词、社区、AI 角色。
- AI 应用不是单点聊天，而是分为“站内导航助手”“沉浸式角色扮演”“课程 AI 命题”三类。
- 社区支持兴趣推荐、词云筛选、媒体上传、多种反应和帖子收藏。
- 首页沉浸式导览结合 Canvas、WebGL、3D 模型和古建戏台数据。
- 后台管理区覆盖内容维护、AI 角色、用户权限、审核和数据看板。
- 数据模型可支撑学习平台、内容平台和社区平台三类场景。

## 十三、可继续完善的方向

- 将敏感配置迁移到环境变量或 Secret 管理。
- 将 `EnsureCreated` 与手写 Schema Patcher 逐步迁移到标准 EF Core Migration 流程。
- 为 AI 调用增加超时、重试、限流和审计日志。
- 为社区上传增加更严格的安全校验。
- 为主要 API 增加自动化测试。
- 将前端大型脚本模块化，降低首页脚本维护成本。
- 对 `Like` 的多态外键设计增加更明确的唯一索引或拆表策略，减少后续扩展时的歧义。

