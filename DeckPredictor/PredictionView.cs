﻿using HearthMirror;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System;
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace DeckPredictor
{
	public class PredictionView
	{
		private PredictionLayout _layout;
		private bool _lastHideOpponentCards;
		private bool _enabled;
		private bool _showing;
		private bool _firstUpdateReceived;
		private User32.MouseInput _mouseInput;

		public PredictionView(PluginConfig _config)
		{
			_layout = new PredictionLayout(_config);
			_mouseInput = new User32.MouseInput();
			_mouseInput.LmbUp += MouseInputOnLmbUp;
			_mouseInput.MouseMoved += MouseInputOnMouseMoved;
			// TestView();
		}

		public void OnUnload()
		{
			_mouseInput.Dispose();
			SetEnabled(false);
		}

		public void SetEnabled(bool enabled)
		{
			if (enabled != _enabled)
			{
				_enabled = enabled;
				if (_enabled)
				{
					Log.Debug("Adding Layout to OverlayCanvas");
					Core.OverlayCanvas.Children.Add(_layout);

					// Turn off the regular Opponent card list and restore the value later.
					_lastHideOpponentCards = Config.Instance.HideOpponentCards;
					Config.Instance.HideOpponentCards = true;
					_firstUpdateReceived = false;
				}
				else
				{
					Log.Debug("Removing Layout from OverlayCanvas");
					Core.OverlayCanvas.Children.Remove(_layout);
					Config.Instance.HideOpponentCards = _lastHideOpponentCards;
				}
			}
			UpdateShowing();
		}

		public void OnPredictionUpdate(PredictionInfo prediction)
		{
			_layout.Dispatcher.Invoke(() =>
				{
					_layout.Update(prediction);
				});
			_firstUpdateReceived = true;
			UpdateShowing();
		}

		private void UpdateShowing()
		{
			bool shouldShow = _enabled && _firstUpdateReceived && !Reflection.IsFriendsListVisible();
			if (shouldShow != _showing)
			{
				Log.Debug("Set Layout Showing: " + shouldShow);
			}
			_showing = shouldShow;
			_layout.Dispatcher.Invoke(() =>
				_layout.Visibility = _showing ? Visibility.Visible : Visibility.Hidden);
		}

		private async void MouseInputOnLmbUp(object sender, EventArgs eventArgs)
		{
			await Task.Delay(100);
			UpdateShowing();
		}

		private void MouseInputOnMouseMoved(object sender, EventArgs eventArgs)
		{
			if (!_showing)
			{
				return;
			}
			var pos = User32.GetMousePos();
			_layout.Dispatcher.Invoke(() =>
				{
					if (_layout.IsLoaded)
					{
						_layout.UpdateCardToolTip(new Point(pos.X, pos.Y));
					}
				});
		}

		private void TestView()
		{
			var cardList = new List<PredictionInfo.CardInfo>();
			for (int n = 0; n < 2; n++)
			{
				var card0 = Database.GetCardFromName("Ice Block");
				card0.Count = 3;
				var cardInfo0 = new PredictionInfo.CardInfo(card0, 0); // OffMeta, Unplayed
				var card1 = Database.GetCardFromName("Ice Block");
				var cardInfo1 = new PredictionInfo.CardInfo(card1, 1); // OffMeta, Played
				var card2 = Database.GetCardFromName("Ice Block");
				card2.Count = 2;
				var cardInfo2 = new PredictionInfo.CardInfo(card2, new List<decimal> {1, 1}, 0); // Unplayed
				var card3 = Database.GetCardFromName("Ice Block");
				card3.IsCreated = true;
				var cardInfo3 = new PredictionInfo.CardInfo(card3, 1); // Created and Played
				var card4 = Database.GetCardFromName("Cryomancer");
				var cardInfo4 = new PredictionInfo.CardInfo(card4, new List<decimal> {1, 1}, 0);
				var card5 = Database.GetCardFromName("Polymorph");
				var cardInfo5 = new PredictionInfo.CardInfo(card5, new List<decimal> {.5m, 1}, 0);
				cardList.Add(cardInfo0);
				cardList.Add(cardInfo1);
				cardList.Add(cardInfo2);
				cardList.Add(cardInfo3);
				cardList.Add(cardInfo4);
				cardList.Add(cardInfo5);
			}
			cardList = cardList
				.OrderBy(cardInfo => cardInfo.Card.Cost)
				.ThenBy(cardInfo => cardInfo.Card.Name)
				.ToList();
			var prediction = new PredictionInfo(1, 30, 4, 5, cardList, new List<PredictionInfo.CardInfo> {});
			SetEnabled(true);
			OnPredictionUpdate(prediction);
		}
	}
}
