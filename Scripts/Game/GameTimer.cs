using System;

public class GameTimer
{
	private double remainingSeconds;

	public double RemainingSeconds => remainingSeconds;

	public bool IsRunning { get; private set; }

	public bool IsFinished => remainingSeconds <= 0;

	public event Action<double> TimeChanged;
	public event Action TimerFinished;

	public GameTimer(double durationSeconds)
	{
		remainingSeconds = durationSeconds;
	}

	public void Start()
	{
		if (IsFinished)
			return;

		IsRunning = true;
	}

	public void Stop()
	{
		IsRunning = false;
	}

	public void Update(double delta)
	{
		if (!IsRunning)
			return;

		remainingSeconds -= delta;

		if (remainingSeconds <= 0)
		{
			remainingSeconds = 0;
			IsRunning = false;

			TimeChanged?.Invoke(remainingSeconds);
			TimerFinished?.Invoke();

			return;
		}

		TimeChanged?.Invoke(remainingSeconds);
	}
}
