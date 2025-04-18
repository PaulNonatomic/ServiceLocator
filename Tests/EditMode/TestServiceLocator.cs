﻿using Nonatomic.ServiceLocator;

namespace Tests.EditMode
{
	internal class TestService
	{
	}

	internal class AnotherTestService
	{
	}

	internal class ThirdTestService
	{
	}

	internal class TestServiceLocator : BaseServiceLocator
	{
		public new void OnEnable()
		{
			base.OnEnable();
		}

		public new void OnDisable()
		{
			base.OnDisable();
		}

		public void ForceInitialize()
		{
			Initialize();
			IsInitialized = true;
		}

		public void ForceDeInitialize()
		{
			DeInitialize();
			IsInitialized = false;
		}

		// Override to prevent automatic initialization
		protected override void Initialize()
		{
			// Do nothing, allowing manual control in tests
		}
	}
}