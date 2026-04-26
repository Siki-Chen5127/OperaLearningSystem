using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace OperaLearningSystem.Infrastructure.Data;

/// <summary>
/// 项目现用 EnsureCreated，无法自动给已存在库加列/加表；启动时执行幂等脚本补齐课程考卷相关结构。
/// </summary>
public static class CourseQuizSchemaPatcher
{
    public static async Task EnsureAsync(OperaDbContext db, CancellationToken ct = default)
    {
        const string sql = @"
IF COL_LENGTH(N'dbo.QuizQuestions', N'CourseId') IS NULL
    ALTER TABLE dbo.QuizQuestions ADD CourseId int NULL;

IF COL_LENGTH(N'dbo.QuizQuestions', N'Explanation') IS NULL
    ALTER TABLE dbo.QuizQuestions ADD Explanation nvarchar(max) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_QuizQuestions_Courses_CourseId')
BEGIN
    ALTER TABLE dbo.QuizQuestions WITH CHECK
    ADD CONSTRAINT FK_QuizQuestions_Courses_CourseId FOREIGN KEY (CourseId) REFERENCES dbo.Courses (Id) ON DELETE CASCADE;
END

IF OBJECT_ID(N'dbo.UserCourseQuizSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserCourseQuizSessions (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_UserCourseQuizSessions PRIMARY KEY,
        UserId int NOT NULL,
        CourseId int NOT NULL,
        QuestionIdsJson nvarchar(max) NOT NULL,
        CurrentIndex int NOT NULL CONSTRAINT DF_UCQS_CurrentIndex DEFAULT (0),
        CorrectCount int NOT NULL CONSTRAINT DF_UCQS_CorrectCount DEFAULT (0),
        WrongCount int NOT NULL CONSTRAINT DF_UCQS_WrongCount DEFAULT (0),
        CreatedAt datetime2 NOT NULL,
        ExpiresAt datetime2 NOT NULL,
        CONSTRAINT FK_UserCourseQuizSessions_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserCourseQuizSessions_Courses_CourseId FOREIGN KEY (CourseId) REFERENCES dbo.Courses (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_UserCourseQuizSessions_ExpiresAt ON dbo.UserCourseQuizSessions (ExpiresAt);
END

IF OBJECT_ID(N'dbo.UserCourseQuizAttempts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserCourseQuizAttempts (
        Id int NOT NULL IDENTITY(1,1) CONSTRAINT PK_UserCourseQuizAttempts PRIMARY KEY,
        UserId int NOT NULL,
        CourseId int NOT NULL,
        CorrectCount int NOT NULL,
        TotalCount int NOT NULL,
        FinishedAt datetime2 NOT NULL,
        CONSTRAINT FK_UserCourseQuizAttempts_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserCourseQuizAttempts_Courses_CourseId FOREIGN KEY (CourseId) REFERENCES dbo.Courses (Id)
    );
    CREATE INDEX IX_UserCourseQuizAttempts_UserId_FinishedAt ON dbo.UserCourseQuizAttempts (UserId, FinishedAt);
END

IF COL_LENGTH(N'dbo.Comments', N'ParentCommentId') IS NULL
    ALTER TABLE dbo.Comments ADD ParentCommentId int NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Comments_ParentCommentId' AND object_id = OBJECT_ID(N'dbo.Comments'))
    CREATE INDEX IX_Comments_ParentCommentId ON dbo.Comments (ParentCommentId);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Comments_Comments_ParentCommentId')
BEGIN
    ALTER TABLE dbo.Comments WITH CHECK ADD CONSTRAINT FK_Comments_Comments_ParentCommentId
        FOREIGN KEY (ParentCommentId) REFERENCES dbo.Comments (Id);
END

IF OBJECT_ID(N'dbo.CommentVotes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CommentVotes (
        Id int NOT NULL IDENTITY(1,1) CONSTRAINT PK_CommentVotes PRIMARY KEY,
        UserId int NOT NULL,
        CommentId int NOT NULL,
        Value smallint NOT NULL,
        CONSTRAINT CK_CommentVotes_Value CHECK (Value IN (-1, 1)),
        CONSTRAINT FK_CommentVotes_AspNetUsers FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id),
        CONSTRAINT FK_CommentVotes_Comments FOREIGN KEY (CommentId) REFERENCES dbo.Comments (Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX UX_CommentVotes_User_Comment ON dbo.CommentVotes (UserId, CommentId);
END
";
        await db.Database.ExecuteSqlRawAsync(sql, ct);
    }

    /// <summary>
    /// 当库结构与模型不一致（例如仅用 dotnet watch 热重载、未重新执行 Program 启动脚本）时，
    /// SQL Server 会报 208「对象名无效」。此时执行幂等 <see cref="EnsureAsync"/> 后重试一次。
    /// </summary>
    public static async Task<TResult> ExecuteWithSchemaRepairAsync<TResult>(
        OperaDbContext db,
        Func<Task<TResult>> operation,
        CancellationToken ct = default)
    {
        try
        {
            return await operation();
        }
        catch (SqlException ex) when (ex.Number is 208 or 1785)
        {
            await EnsureAsync(db, ct);
            return await operation();
        }
    }
}
