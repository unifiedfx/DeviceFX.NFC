using Android.Gms.Tasks;
using Object = Java.Lang.Object;

namespace DeviceFX.NfcApp;

public static class TaskExtensions
{
    private class TaskCompleteListener(TaskCompletionSource<Object> tcs) : Object, IOnCompleteListener
    {
        public void OnComplete(Android.Gms.Tasks.Task task)
        {
            if (task.IsCanceled)
            {
                tcs.SetCanceled();
            }
            else if (task.IsSuccessful)
            {
                tcs.SetResult(task.Result);
            }
            else
            {
                tcs.SetException(task.Exception);
            }
        }
    }

    public static Task<Object> ToAwaitableTask(this Android.Gms.Tasks.Task task)
    {
        var taskCompletionSource = new TaskCompletionSource<Object>();
        var taskCompleteListener = new TaskCompleteListener(taskCompletionSource);
        task.AddOnCompleteListener(taskCompleteListener);
        return taskCompletionSource.Task;
    }
}