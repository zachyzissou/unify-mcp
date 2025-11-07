using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnifyMcp.Core.Protocol;

namespace UnifyMcp.Tools.Unity
{
    /// <summary>
    /// MCP tools for querying Unity scene hierarchy and GameObjects.
    /// Allows AI agents to inspect the scene structure, find objects, and read component data.
    /// </summary>
    public class SceneQueryTools
    {
        /// <summary>
        /// Gets the complete scene hierarchy with configurable depth limit.
        /// </summary>
        [McpTool("get_scene_hierarchy", "Get Unity scene hierarchy with GameObject names, components, and children")]
        public async Task<string> GetSceneHierarchy(int maxDepth = 3)
        {
            return await Task.Run(() =>
            {
                var scene = SceneManager.GetActiveScene();
                var rootObjects = scene.GetRootGameObjects();

                var hierarchy = rootObjects.Select(obj =>
                    SerializeGameObject(obj, 0, maxDepth)).ToArray();

                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    sceneName = scene.name,
                    scenePath = scene.path,
                    isLoaded = scene.isLoaded,
                    rootObjectCount = rootObjects.Length,
                    hierarchy
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            });
        }

        /// <summary>
        /// Finds a GameObject by name and returns detailed information.
        /// </summary>
        [McpTool("find_game_object", "Find a GameObject by name and get its properties")]
        public async Task<string> FindGameObject(string name)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(name))
                {
                    return System.Text.Json.JsonSerializer.Serialize(new
                    {
                        found = false,
                        error = "Name parameter is required"
                    });
                }

                var obj = GameObject.Find(name);
                if (obj == null)
                {
                    return System.Text.Json.JsonSerializer.Serialize(new
                    {
                        found = false,
                        searchedName = name
                    });
                }

                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    found = true,
                    name = obj.name,
                    tag = obj.tag,
                    layer = LayerMask.LayerToName(obj.layer),
                    activeInHierarchy = obj.activeInHierarchy,
                    activeSelf = obj.activeSelf,
                    scene = obj.scene.name,
                    isStatic = obj.isStatic,
                    transform = new
                    {
                        position = new
                        {
                            x = obj.transform.position.x,
                            y = obj.transform.position.y,
                            z = obj.transform.position.z
                        },
                        rotation = new
                        {
                            x = obj.transform.rotation.eulerAngles.x,
                            y = obj.transform.rotation.eulerAngles.y,
                            z = obj.transform.rotation.eulerAngles.z
                        },
                        scale = new
                        {
                            x = obj.transform.localScale.x,
                            y = obj.transform.localScale.y,
                            z = obj.transform.localScale.z
                        }
                    },
                    components = obj.GetComponents<Component>()
                        .Where(c => c != null)
                        .Select(c => new
                        {
                            type = c.GetType().Name,
                            fullType = c.GetType().FullName,
                            enabled = (c is Behaviour behaviour) ? behaviour.enabled : true
                        }).ToArray(),
                    childCount = obj.transform.childCount,
                    parent = obj.transform.parent != null ? obj.transform.parent.name : null
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            });
        }

        /// <summary>
        /// Gets statistics about the current scene.
        /// </summary>
        [McpTool("get_scene_statistics", "Get statistics about GameObjects and components in the scene")]
        public async Task<string> GetSceneStatistics()
        {
            return await Task.Run(() =>
            {
                var scene = SceneManager.GetActiveScene();
                var allObjects = scene.GetRootGameObjects()
                    .SelectMany(GetAllGameObjects).ToArray();

                var componentCounts = allObjects
                    .SelectMany(obj => obj.GetComponents<Component>())
                    .Where(c => c != null)
                    .GroupBy(c => c.GetType().Name)
                    .OrderByDescending(g => g.Count())
                    .Take(15)
                    .ToDictionary(g => g.Key, g => g.Count());

                var tagCounts = allObjects
                    .GroupBy(obj => obj.tag)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count());

                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    sceneName = scene.name,
                    scenePath = scene.path,
                    totalGameObjects = allObjects.Length,
                    activeGameObjects = allObjects.Count(obj => obj.activeInHierarchy),
                    inactiveGameObjects = allObjects.Count(obj => !obj.activeInHierarchy),
                    rootObjects = scene.GetRootGameObjects().Length,
                    topComponents = componentCounts,
                    topTags = tagCounts
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            });
        }

        /// <summary>
        /// Finds all GameObjects with a specific component type.
        /// </summary>
        [McpTool("find_objects_with_component", "Find all GameObjects that have a specific component")]
        public async Task<string> FindObjectsWithComponent(string componentName, int maxResults = 50)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(componentName))
                {
                    return System.Text.Json.JsonSerializer.Serialize(new
                    {
                        found = false,
                        error = "componentName parameter is required"
                    });
                }

                var scene = SceneManager.GetActiveScene();
                var allObjects = scene.GetRootGameObjects()
                    .SelectMany(GetAllGameObjects).ToArray();

                var matchingObjects = allObjects
                    .Where(obj => obj.GetComponents<Component>()
                        .Any(c => c != null && c.GetType().Name.Equals(componentName, StringComparison.OrdinalIgnoreCase)))
                    .Take(maxResults)
                    .Select(obj => new
                    {
                        name = obj.name,
                        path = GetGameObjectPath(obj),
                        active = obj.activeInHierarchy,
                        components = obj.GetComponents<Component>()
                            .Where(c => c != null)
                            .Select(c => c.GetType().Name)
                            .ToArray()
                    })
                    .ToArray();

                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    componentName,
                    foundCount = matchingObjects.Length,
                    objects = matchingObjects
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            });
        }

        /// <summary>
        /// Finds all GameObjects with a specific tag.
        /// </summary>
        [McpTool("find_objects_by_tag", "Find all GameObjects with a specific tag")]
        public async Task<string> FindObjectsByTag(string tag)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return System.Text.Json.JsonSerializer.Serialize(new
                    {
                        found = false,
                        error = "tag parameter is required"
                    });
                }

                var objects = GameObject.FindGameObjectsWithTag(tag);

                var results = objects.Select(obj => new
                {
                    name = obj.name,
                    path = GetGameObjectPath(obj),
                    active = obj.activeInHierarchy,
                    components = obj.GetComponents<Component>()
                        .Where(c => c != null)
                        .Select(c => c.GetType().Name)
                        .ToArray()
                }).ToArray();

                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    tag,
                    foundCount = results.Length,
                    objects = results
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            });
        }

        /// <summary>
        /// Gets a list of all loaded scenes.
        /// </summary>
        [McpTool("get_loaded_scenes", "Get list of all loaded Unity scenes")]
        public async Task<string> GetLoadedScenes()
        {
            return await Task.Run(() =>
            {
                var scenes = new List<object>();

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    scenes.Add(new
                    {
                        name = scene.name,
                        path = scene.path,
                        isLoaded = scene.isLoaded,
                        isActive = SceneManager.GetActiveScene() == scene,
                        rootCount = scene.GetRootGameObjects().Length,
                        buildIndex = scene.buildIndex
                    });
                }

                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    sceneCount = scenes.Count,
                    activeScene = SceneManager.GetActiveScene().name,
                    scenes
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            });
        }

        // Helper methods

        private object SerializeGameObject(GameObject obj, int depth, int maxDepth)
        {
            var result = new Dictionary<string, object>
            {
                ["name"] = obj.name,
                ["active"] = obj.activeInHierarchy,
                ["tag"] = obj.tag,
                ["layer"] = LayerMask.LayerToName(obj.layer),
                ["components"] = obj.GetComponents<Component>()
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name).ToArray()
            };

            if (depth < maxDepth && obj.transform.childCount > 0)
            {
                var children = new List<object>();
                foreach (Transform child in obj.transform)
                {
                    if (child != null && child.gameObject != null)
                    {
                        children.Add(SerializeGameObject(child.gameObject, depth + 1, maxDepth));
                    }
                }
                result["children"] = children;
            }
            else if (obj.transform.childCount > 0)
            {
                result["childCount"] = obj.transform.childCount;
            }

            return result;
        }

        private IEnumerable<GameObject> GetAllGameObjects(GameObject root)
        {
            if (root == null) yield break;

            yield return root;

            foreach (Transform child in root.transform)
            {
                if (child != null && child.gameObject != null)
                {
                    foreach (var obj in GetAllGameObjects(child.gameObject))
                    {
                        yield return obj;
                    }
                }
            }
        }

        private string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return "";

            var path = obj.name;
            var current = obj.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
