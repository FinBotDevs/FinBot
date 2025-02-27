﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace FinBot.Services
{
	public class ReminderService : ModuleBase<ShardedCommandContext>
    {
        public static DiscordShardedClient _client;

		public ReminderService(IServiceProvider service)
		{
			_client = service.GetRequiredService<DiscordShardedClient>();
         
            Timer t = new Timer() { AutoReset = true, Interval = new TimeSpan(0, 0, 0, 10).TotalMilliseconds, Enabled = true };
            t.Enabled = true;
            t.Elapsed += RemindAsync;
            t.Start();
        }

        /// <summary>
        /// Checks the database for any reminders to send.
        /// </summary>
        /// <param name="sender">Timer-generated variable.</param>
        /// <param name="e">Timer-generated variable.</param>
        public async void RemindAsync(object sender, ElapsedEventArgs e)
        {
            MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);
            MySqlConnection QueryConn = new MySqlConnection(Global.MySQL.ConnStr);

            try
            {
                conn.Open();
                long Now = Global.ConvertToTimestamp(DateTime.Now);
                MySqlCommand cmd1 = new MySqlCommand($"SELECT * FROM Reminders WHERE {Now} > reminderTimestamp", conn);
                MySqlDataReader reader = cmd1.ExecuteReader();

                while (reader.Read())
                {
                    SocketGuild guild = _client.GetGuild(reader.GetUInt64(1));
                    SocketUser user = guild.GetUser(reader.GetUInt64(0));
                    SocketTextChannel channel = guild.GetTextChannel(reader.GetUInt64(2));
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithTitle("Reminder");
                    eb.WithDescription($"{reader.GetString(5)}");
                    eb.WithAuthor(user);
                    eb.WithFooter($"Reminder set at {Global.UnixTimeStampToDateTime(reader.GetInt64(3))}");
                    eb.WithCurrentTimestamp();
                    await channel.SendMessageAsync("", false, eb.Build());
                    QueryConn.Open();
                    await InsertToDBAsync(1, QueryConn, user.Id, guild.Id);
                    QueryConn.Close();
                }

                conn.Close();
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.Message);
            }

            finally
            {
                conn.Close();
            }
        }

        /// <summary>
        /// Inserts a reminder into the database.
        /// </summary>
        /// <param name="type">The option for what kind of interaction is made with the database.</param>
        /// <param name="conn">The database connection string.</param>
        /// <param name="userId">The id of the user who set the reminder.</param>
        /// <param name="guildId">The id of the guild where the reminder was set.</param>
        /// <param name="nowTimestamp">The unix timestamp of when the reminder was set.</param>
        /// <param name="reminderTimestamp">The unix timestamp of when to remind the user.</param>
        /// <param name="message">The message to send alongside the reminder.</param>
        /// <param name="chan">The channel where the reminder was set.</param>
        public async static Task InsertToDBAsync(uint type, MySqlConnection conn, ulong userId, ulong guildId, long nowTimestamp = 0, long reminderTimestamp = 0, string message = null, SocketTextChannel chan = null)
        {
            try
            {
                if (type == 0)
                {
                    MySqlCommand cmd = new MySqlCommand($"INSERT INTO Reminders(userId, guildId, chanId, timeSet, reminderTimestamp, message) VALUES ({userId}, {guildId}, {chan.Id}, {nowTimestamp}, {reminderTimestamp}, '{message}')", conn);
                    cmd.ExecuteNonQuery();
                }

                else
                {
                    MySqlCommand cmd = new MySqlCommand($"DELETE FROM Reminders where userId = {userId} AND guildId = {guildId}", conn);
                    cmd.ExecuteNonQuery();
                }
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.Message);
                await chan.SendMessageAsync(ex.Message);
            }
        }

        /// <summary>
        /// Sets a user-set reminder.
        /// </summary>
        /// <param name="guild">The guild where the reminder was set.</param>
        /// <param name="user">The user who set the reminder.</param>
        /// <param name="chan">The channel where the reminder was set.</param>
        /// <param name="timeSet">The current time(when the reminder was set).</param>
        /// <param name="duration">The duration for the reminder timer.</param>
        /// <param name="message">The reminder message.</param>
        /// <param name="context">The context of the message to set the reminder.</param>
        public async static Task SetReminder(SocketGuild guild, SocketUser user, SocketTextChannel chan, DateTime timeSet, string duration, string message, ShardedCommandContext context)
        {
            long currentTime = Global.ConvertToTimestamp(timeSet);
            TimeSpan time = TimeSpan.FromSeconds(Convert.ToInt64(await Parse_time(duration)));
            DateTime remindertime = DateTime.Now + time;
            long reminderTimestamp = Global.ConvertToTimestamp(remindertime);
            MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);
            MySqlConnection QueryConn = new MySqlConnection(Global.MySQL.ConnStr);

            try
            {
                conn.Open();
                bool read = false;
                MySqlCommand cmd1 = new MySqlCommand($"SELECT * FROM Reminders WHERE userId = {user.Id} AND guildId = {guild.Id}", conn);
                MySqlDataReader reader = (MySqlDataReader)await cmd1.ExecuteReaderAsync();

                while (reader.Read())
                {
                    read = true;
                    await chan.SendMessageAsync($"You already have a timer active. Please try again after this has expired or stop the timer by using the {await Global.DeterminePrefix(context)}stopreminder command.");
                }

                if (!read)
                {
                    await chan.SendMessageAsync($"Set a reminder with message \"{message}\" for {duration}");
                }

                conn.Close();
                QueryConn.Open();
                await InsertToDBAsync(0, QueryConn, user.Id, guild.Id, currentTime, reminderTimestamp, message, chan);
                QueryConn.Close();
            }

            catch (Exception ex)
            {
                //if (ex.Message.GetType() != typeof(NullReferenceException))
                //{
                //    EmbedBuilder eb = new EmbedBuilder();
                //    eb.WithAuthor(user);
                //    eb.WithTitle("Error setting reminder message:");
                //    eb.WithDescription($"The database returned an error code:{ex.Message}\n{ex.Source}\n{ex.StackTrace}\n{ex.TargetSite}");
                //    eb.WithCurrentTimestamp();
                //    eb.WithColor(Color.Red);
                //    eb.WithFooter("Please DM the bot ```support <issue>``` about this error and the developers will look at your ticket");
                //    await chan.SendMessageAsync("", false, eb.Build());
                //    return;
                //}
                Global.ConsoleLog(ex.Message);
            }

            finally
            {
                conn.Close();
            }
        }

        /// <summary>
        /// Converts a string of how long to mute a user for into seconds, e.g: "1h" will return "3600";
        /// </summary>
        /// <param name="time">The duration for how long to mute the user for.</param>
        /// <returns>Returns the duration in seconds.</returns>
        public static Task<string> Parse_time(string time)
        {
            time = "time " + time;
            float result = 0.0f;
            int len = time.Length;

            for (int i = len - 1; i > 0; i--)
            {
                float _base;

                switch (time[i])
                {
                    case 's':
                        _base = 1.0f;
                        break;
                    case 'm':
                        _base = 60.0f;
                        break;
                    case 'h':
                        _base = 60.0f * 60.0f;
                        break;
                    case 'd':
                        _base = 60.0f * 60.0f * 24;
                        break;
                    default:
                        continue;
                }

                float exponent = 1.0f;

                for (int j = 1; j <= i + 1; j++)
                {
                    if (char.IsDigit((time[i - j])))
                    {
                        result += (time[i - j] - '0') * _base * exponent;
                        exponent *= 10.0f;
                    }

                    else
                    {
                        break;
                    }
                }
            }

            if (result > 0.0f)
            {
                return Task.FromResult(result.ToString());
            }

            else
            {
                return Task.FromResult(time);
            }
        }
    }
}
