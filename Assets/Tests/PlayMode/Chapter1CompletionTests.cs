using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class Chapter1CompletionTests
{
    private static readonly MethodInfo TryMoveMethod =
        typeof(Player).GetMethod("TryMove", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo IsRollingField =
        typeof(Player).GetField("isRolling", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo IsFallingField =
        typeof(Player).GetField("isFalling", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo IsExternallyControlledField =
        typeof(Player).GetField("isExternallyControlled", BindingFlags.Instance | BindingFlags.NonPublic);

    private string savePath;
    private bool saveExisted;
    private byte[] savedProgress;
    private bool originalRunInBackground;

    [OneTimeSetUp]
    public void PreserveProgress()
    {
        originalRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        savePath = Path.Combine(Application.persistentDataPath, "levelprogress.json");
        saveExisted = File.Exists(savePath);
        if (saveExisted) savedProgress = File.ReadAllBytes(savePath);
    }

    [OneTimeTearDown]
    public void RestoreProgress()
    {
        Application.runInBackground = originalRunInBackground;
        if (saveExisted)
            File.WriteAllBytes(savePath, savedProgress);
        else if (File.Exists(savePath))
            File.Delete(savePath);
    }

    [UnityTest]
    public IEnumerator Level01_PrimaryRouteCompletes()
    {
        yield return LoadLevel(1);
        Player player = Object.FindAnyObjectByType<Player>();
        yield return MoveMany(player, Vector3.right, Vector3.right, Vector3.right, Vector3.right,
            Vector3.right, Vector3.right, Vector3.forward, Vector3.left, Vector3.back);
        yield return ExpectTransition("Chapter1_Scene2");
    }

    [UnityTest]
    public IEnumerator Level02_PrimaryRouteCompletes()
    {
        yield return LoadLevel(2);
        Player player = Object.FindAnyObjectByType<Player>();
        yield return MoveMany(player, Vector3.right, Vector3.right, Vector3.right);
        yield return WaitForPlayerReady(player, 4f);
        yield return MoveMany(player, Vector3.right, Vector3.right, Vector3.right,
            Vector3.forward, Vector3.left, Vector3.back);
        yield return ExpectTransition("Chapter1_Scene3");
    }

    [UnityTest]
    public IEnumerator Level03_PrimaryRouteCompletes()
    {
        yield return LoadLevel(3);
        Player player = Object.FindAnyObjectByType<Player>();
        yield return MoveMany(player, Vector3.right, Vector3.right, Vector3.forward);
        yield return new WaitForSeconds(3.2f);
        yield return MoveMany(player, Vector3.right, Vector3.back, Vector3.right, Vector3.right,
            Vector3.right, Vector3.right);
        yield return ExpectTransition("Chapter1_Scene4");
    }

    [UnityTest]
    public IEnumerator Level04_PrimaryRouteCompletes()
    {
        yield return LoadLevel(4);
        Player player = Object.FindAnyObjectByType<Player>();
        yield return MoveMany(player, Vector3.right, Vector3.right, Vector3.right, Vector3.right);
        yield return new WaitForSeconds(2.3f);
        yield return WaitForPlayerReady(player, 4f);
        Assert.That(player.transform.position.x, Is.EqualTo(2f).Within(0.05f),
            "carrier_platform did not deliver the player to x=+02.");
        yield return MoveMany(player, Vector3.right, Vector3.right);
        yield return ExpectTransition("Chapter1_Scene5");
    }

    [UnityTest]
    public IEnumerator Level05_PrimaryRouteCompletes()
    {
        yield return LoadLevel(5);
        Player player = Object.FindAnyObjectByType<Player>();
        yield return MoveMany(player, Vector3.right, Vector3.right, Vector3.forward);
        yield return new WaitForSeconds(3.2f);
        yield return MoveMany(player, Vector3.back, Vector3.right, Vector3.right, Vector3.right);
        yield return new WaitForSeconds(2.3f);
        yield return WaitForPlayerReady(player, 4f);
        Assert.That(player.transform.position.x, Is.EqualTo(2f).Within(0.05f),
            "returning_platform did not deliver the player to x=+02.");
        yield return MoveMany(player, Vector3.right, Vector3.right, Vector3.right,
            Vector3.right, Vector3.right);
        yield return ExpectTransition("Chapter1_Scene6");
    }

    [UnityTest]
    public IEnumerator Level06_PrimaryRouteCompletes()
    {
        yield return LoadLevel(6);
        Player player = Object.FindAnyObjectByType<Player>();
        yield return MoveMany(player, Vector3.right, Vector3.right, Vector3.forward, Vector3.right,
            Vector3.right, Vector3.forward, Vector3.right, Vector3.forward, Vector3.right);
        yield return ExpectTransition("Chapter1_Scene7");
    }

    [UnityTest]
    public IEnumerator Level07_PrimaryRouteCompletes()
    {
        yield return LoadLevel(7);
        Player player = Object.FindAnyObjectByType<Player>();
        yield return MoveMany(player, Vector3.right, Vector3.right);
        yield return WaitForBox("horizontal_box");
        yield return MoveMany(player, Vector3.right, Vector3.right, Vector3.forward, Vector3.forward);
        yield return WaitForBox("vertical_box");
        yield return MoveMany(player, Vector3.forward, Vector3.forward, Vector3.right,
            Vector3.forward, Vector3.right);
        yield return ExpectTransition("Chapter1_Scene8");
    }

    [UnityTest]
    public IEnumerator Level08_PrimaryRouteCompletes()
    {
        yield return LoadLevel(8);
        Player player = Object.FindAnyObjectByType<Player>();
        yield return MoveMany(player, Vector3.right, Vector3.back);
        yield return new WaitForSeconds(2.2f);
        yield return MoveMany(player, Vector3.right, Vector3.right, Vector3.back,
            Vector3.right, Vector3.right, Vector3.right, Vector3.forward,
            Vector3.right, Vector3.back);
        yield return ExpectTransition("Chapter1_Scene9");
    }

    [UnityTest]
    public IEnumerator Level09_PrimaryRouteCompletes()
    {
        yield return LoadLevel(9);
        Player player = Object.FindAnyObjectByType<Player>();
        yield return MoveMany(player, Vector3.right, Vector3.right, Vector3.right);
        yield return WaitForBox("key_box");
        yield return new WaitForSeconds(1.6f);
        yield return MoveMany(player, Vector3.forward, Vector3.forward, Vector3.right,
            Vector3.right, Vector3.right, Vector3.right, Vector3.right, Vector3.right,
            Vector3.right);

        yield return new WaitForSeconds(2.3f);
        Assert.AreEqual("Chapter1_Scene9", SceneManager.GetActiveScene().name);
        Assert.IsTrue(LevelProgress.Instance.IsUnlocked(1, 10),
            "Completing Chapter1_Scene9 should register level 9 completion.");
    }

    private static IEnumerator LoadLevel(int level)
    {
        SceneManager.LoadScene($"Chapter1_Scene{level}");
        yield return null;
        yield return null;
        Assert.AreEqual($"Chapter1_Scene{level}", SceneManager.GetActiveScene().name);
        Assert.IsNotNull(Object.FindAnyObjectByType<Player>());
        Assert.IsNotNull(Object.FindAnyObjectByType<SceneSwitcher>());

        // Skip straight to the ripple entry animation's end state: primary-route tests
        // drive the player via direct TryMove calls (bypassing Update()'s input lock), so
        // they'd otherwise start moving while terrain colliders are still mid pop-in.
        LevelEntryAnimator entryAnimator = Object.FindAnyObjectByType<LevelEntryAnimator>();
        Assert.IsNotNull(entryAnimator);
        entryAnimator.SkipToComplete();
    }

    private static IEnumerator MoveMany(Player player, params Vector3[] directions)
    {
        foreach (Vector3 direction in directions)
            yield return Move(player, direction);
    }

    private static IEnumerator Move(Player player, Vector3 direction)
    {
        Assert.IsNotNull(TryMoveMethod);
        IEnumerator movement = (IEnumerator)TryMoveMethod.Invoke(player, new object[] { direction });
        player.StartCoroutine(movement);
        yield return null;
        yield return WaitForPlayerReady(player, 6f);
    }

    private static IEnumerator WaitForPlayerReady(Player player, float timeout)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (IsBusy(player) && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.IsFalse(IsBusy(player), $"Player did not become ready within {timeout:0.0}s.");
        yield return null;
    }

    private static bool IsBusy(Player player)
    {
        return (bool)IsRollingField.GetValue(player)
            || (bool)IsFallingField.GetValue(player)
            || (bool)IsExternallyControlledField.GetValue(player);
    }

    private static IEnumerator WaitForBox(string name)
    {
        GameObject box = GameObject.Find(name);
        Assert.IsNotNull(box);
        Rigidbody body = box.GetComponent<Rigidbody>();
        float deadline = Time.realtimeSinceStartup + 5f;
        while ((body.linearVelocity.sqrMagnitude > 0.0025f || box.transform.position.y > 0f)
               && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.LessOrEqual(box.transform.position.y, -0.45f, $"{name} did not fall into its pit.");
        Assert.Less(body.linearVelocity.sqrMagnitude, 0.0025f, $"{name} did not settle.");
        yield return new WaitForSeconds(0.1f);
    }

    private static IEnumerator ExpectTransition(string targetScene)
    {
        float deadline = Time.realtimeSinceStartup + 4f;
        while (SceneManager.GetActiveScene().name != targetScene && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.AreEqual(targetScene, SceneManager.GetActiveScene().name);
    }
}
