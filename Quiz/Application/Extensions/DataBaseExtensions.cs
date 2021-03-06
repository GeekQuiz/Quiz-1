﻿using System;
using System.Collections.Generic;
using System.Linq;
using Application.Repositories;
using Application.Repositories.Entities;
using Infrastructure.Extensions;

namespace Application.Extensions
{
    public static class DataBaseExtensions
    {
        public static UserEntity FindOrInsertUser(
            this IUserRepository userRepository,
            Guid userId,
            ITaskRepository taskRepository)
        {
            var progress = new UserProgressEntity(
                Guid.NewGuid(),
                Guid.NewGuid(),
                userId,
                id: Guid.NewGuid(),
                currentTask: null,
                topicsProgress: taskRepository
                    .GetTopics()
                    .SafeToDictionary(
                        topic => topic.Id,
                        topic => topic.ToProgressEntity()));

            return userRepository.FindById(userId) ?? userRepository.Insert(new UserEntity(userId, progress));
        }

        public static int GetCurrentStreak(
            this UserEntity user,
            Guid? topicId = null,
            Guid? levelId = null,
            Guid? generatorId = null)
        {
            var progress = user.UserProgressEntity;
            return progress
                .TopicsProgress[topicId ?? progress.CurrentTopicId]
                .LevelProgressEntities[levelId ?? progress.CurrentLevelId]
                .CurrentLevelStreaks[generatorId ?? progress.CurrentTask.ParentGeneratorId];
        }

        public static bool TopicExists(this ITaskRepository taskRepository, Guid topicId) =>
            taskRepository.FindTopic(topicId) != null;

        public static bool LevelExists(this ITaskRepository taskRepository, Guid topicId, Guid levelId) =>
            taskRepository.FindLevel(topicId, levelId) != null;

        public static bool GeneratorExists(
            this ITaskRepository taskRepository,
            Guid topicId,
            Guid levelId,
            Guid generatorId)
        {
            return taskRepository.FindGenerator(topicId, levelId, generatorId) != null;
        }

        public static UserProgressEntity GetRelevantUserProgress(
            this UserProgressEntity userProgress,
            ITaskRepository taskRepository)
        {
            var topics = taskRepository.GetTopics();
            foreach (var topic in topics)
                userProgress.TopicsProgress.TryAdd(topic.Id, topic.ToProgressEntity());

            var ids = topics.Select(topic => topic.Id);
            var progress = userProgress
                .TopicsProgress
                .TakeFrom(ids);

            return userProgress.With(topicsProgress: progress);
        }

        public static TopicProgressEntity GetRelevantTopicProgress(
            this TopicProgressEntity topicProgress,
            ITaskRepository taskRepository)
        {
            var levels = taskRepository.GetLevelsFromTopic(topicProgress.TopicId);
            if (levels.Length == 0)
                return topicProgress.With(new Dictionary<Guid, LevelProgressEntity>());

            var firstLevel = levels[0];

            topicProgress
                .LevelProgressEntities
                .TryAdd(firstLevel.Id, firstLevel.ToProgressEntity());

            var ids = levels.Select(level => level.Id);
            var progress = topicProgress
                .LevelProgressEntities
                .TakeFrom(ids);

            return topicProgress.With(progress);
        }

        public static LevelProgressEntity GetRelevantLevelProgress(
            this LevelProgressEntity levelProgress,
            Guid topicId,
            ITaskRepository taskRepository)
        {
            var level = taskRepository.FindLevel(topicId, levelProgress.LevelId);
            if (level is null)
                return levelProgress;

            foreach (var generator in level.Generators)
                levelProgress.CurrentLevelStreaks.TryAdd(generator.Id, 0);

            var ids = level.Generators.Select(generator => generator.Id);
            var streaks = levelProgress
                .CurrentLevelStreaks
                .TakeFrom(ids);

            return levelProgress.With(streaks);
        }

        public static void UpdateUserProgress(
            this IUserRepository userRepository,
            ITaskRepository taskRepository,
            UserEntity user)
        {
            var progress = user.UserProgressEntity.GetRelevantUserProgress(taskRepository);

            userRepository.Update(user.With(progress));
        }

        public static void UpdateTopicProgress(
            this IUserRepository userRepository,
            ITaskRepository taskRepository,
            UserEntity user,
            Guid topicId)
        {
            var topicsProgress = user.UserProgressEntity.TopicsProgress;

            if (!topicsProgress.ContainsKey(topicId))
                return;

            var progress = topicsProgress[topicId]
                .GetRelevantTopicProgress(taskRepository);

            topicsProgress[topicId] = progress;

            userRepository.Update(user);
        }

        public static void UpdateLevelProgress(
            this IUserRepository userRepository,
            ITaskRepository taskRepository,
            UserEntity user,
            Guid topicId,
            Guid levelId)
        {
            var topicsProgress = user.UserProgressEntity.TopicsProgress;

            if (!topicsProgress.ContainsKey(topicId) ||
                !topicsProgress[topicId].LevelProgressEntities.ContainsKey(levelId))
                return;

            var progress = topicsProgress[topicId]
                .LevelProgressEntities[levelId]
                .GetRelevantLevelProgress(topicId, taskRepository);

            topicsProgress[topicId]
                .LevelProgressEntities[levelId] = progress;

            userRepository.Update(user);
        }

        public static bool HasCurrentTask(this UserEntity user) => user.UserProgressEntity.CurrentTask != null;
    }
}