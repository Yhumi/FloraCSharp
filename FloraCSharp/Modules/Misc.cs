﻿using Discord.Commands;
using FloraCSharp.Services;
using FloraCSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using System.Net.Http;
using FloraCSharp.Services.Database.Models;

namespace FloraCSharp.Modules
{
    [RequireContext(ContextType.Guild)]
    class Misc : ModuleBase
    {
        private HttpClient _client;
        private FloraDebugLogger _logger;
        private readonly FloraRandom _random;

        public Misc(FloraRandom random, FloraDebugLogger logger)
        {
            _client = new HttpClient();
            _random = random;
            _logger = logger;
        }

        [Command("Test"), Summary("Simple test command to see if the bot is running")]
        public async Task Test()
        {
            await Context.Channel.SendSuccessAsync("Hello!");
        }

        [Command("RNG"), Summary("Simple random number command")]
        public async Task RNG([Summary("The minimum, inclusive bound")] int min, [Summary("The maximum, exclusive bound")] int max)
        {
            await Context.Channel.SendSuccessAsync($"Random Number (Min : {min}, Max: {max})", $"{_random.Next(min, max)}");
        }

        [Command("RNG"), Summary("Simple random number command")]
        public async Task RNG([Summary("The maximum, exclusive bound")] int max)
        {
            await Context.Channel.SendSuccessAsync($"Random Number (Max: {max-1})", $"{_random.Next(max)}");
        }

        [Command("RNG"), Summary("Simple random number command")]
        public async Task RNG()
        {
            await Context.Channel.SendSuccessAsync("Random Number", $"{_random.Next()}");
        }

        [Command("Tastes"), Summary("Shitpost Generator (Tastes)")]
        public async Task Tastes([Remainder] [Summary("What is the kid's franchise?")] string taste)
        {
            string start = "I know you \"can't discuss taste\" (bollocks), but I'm now ready to admit that ";
            string end = " is a kids' franchise for those with low expectations who are happy as long as they get more.";

            await Context.Channel.SendSuccessAsync(start + taste + end);
        }

        [Command("ProfilePic"), Summary("Gets the profile pic of the user running it OR a specific user")]
        [Alias("pfp")]
        public async Task ProfilePic([Summary("The optional user who's pfp you're after")] IGuildUser user = null)
        {
            if (user == null)
                user = (IGuildUser)Context.User;

            await Context.Channel.SendPictureAsync("Profile Pic", user.Username, user.GetAvatarUrl());
        }

        [Command("PickUser"), Summary("Picks a random user from a role.")]
        [Alias("pur", "pickrole", "raffle")]
        public async Task PickRole([Summary("The role you want to pick the user from. Defaults to everyone.")] string roleName = null)
        {
            IRole role = null;

            if (roleName == null)
                role = Context.Guild.EveryoneRole;
            else
            {
                foreach (IRole rl in Context.Guild.Roles)
                {
                    if (rl.Name.ToLower() == roleName.ToLower())
                    {
                        role = rl;
                        break;
                    }
                }
            }

            if (role != null)
                await PickUserFromRole(role, Context.Guild, Context.Channel);
        }

        [Command("PickUser"), Summary("Picks a random user from a role.")]
        [Alias("pur", "pickrole", "raffle")]
        public async Task PickRole([Summary("The role you want to pick the user from. Defaults to everyone.")] IRole roleName) => await PickUserFromRole(roleName, Context.Guild, Context.Channel);

        private async Task PickUserFromRole(IRole role, IGuild guild, IMessageChannel channel)
        {
            var GUsers = await guild.GetUsersAsync();

            GUsers = new List<IGuildUser>(
                (from user in GUsers where !user.IsBot select user));

            if (role != guild.EveryoneRole)
            {
                List<IGuildUser> PossibleUsers = new List<IGuildUser>(
                    (from user in GUsers where user.RoleIds.Contains(role.Id) select user));

                if (PossibleUsers.Count > 1) 
                    await channel.SendSuccessAsync("Chosen User", PossibleUsers.ElementAt(_random.Next(PossibleUsers.Count)).Username);
                else
                    await channel.SendSuccessAsync("Chosen User", PossibleUsers.First().Username);
            }
            else
            {
                await channel.SendSuccessAsync("Chosen User", GUsers.ElementAt(_random.Next(GUsers.Count)).Username);
            }
        }

        [Command("Choose"), Summary("Picks a random option from the given choices.")]
        public async Task Choose([Remainder] [Summary("Choices")] string options)
        {
            if (!options.Contains(";")) return;
            List <string> list = new List<string>(options.Split(';'));

            await Context.Channel.SendSuccessAsync(list.ElementAt(_random.Next(list.Count)));
        }

        [Command("GDQDonation"), Summary("Gets a randomly generated GDQ donation.")]
        [Alias("GDQD")]
        public async Task GDQDonation()
        {
            string url = "http://taskinoz.com/gdq/api/";
            _client.BaseAddress = new Uri(url);

            //Get response
            HttpResponseMessage resp = _client.GetAsync("").Result;

            if(resp.IsSuccessStatusCode)
            {
                //Parse
                var ResponseText = await resp.Content.ReadAsStringAsync();

                await Context.Channel.SendSuccessAsync(ResponseText);
            }
        }

        [Command("TestUpdate"), Summary("I added this cause I'm a little bitch")]
        [RequireUserPermission(GuildPermission.MentionEveryone)]
        public async Task TestUpdate()
        {
            await Context.Channel.SendSuccessAsync("This shit worked like a charm.");
        }

        [Command("RateUser"), Summary("Rates a given user")]
        [Alias("Rate")]
        public async Task RateUser([Summary("User to rate")] IGuildUser user)
        {
            UserRating rating = null;

            _logger.Log("Command started...", "RateUser");

            using (var uow = DBHandler.UnitOfWork())
            {
                _logger.Log("Getting rating...", "RateUser");
                rating = uow.UserRatings.GetUserRating(user.Id);
            }

            if (rating != null)
            {
                await Context.Channel.SendSuccessAsync("User Rating", $"The rating for {user.Mention} is {rating.Rating}/10");
            }
            else
            {
                string uIDsT = user.Id.ToString();
                char[] uIDcA = uIDsT.ToCharArray();

                int firstChar = Int32.Parse(uIDcA[0].ToString());
                int secondChar = Int32.Parse(uIDcA[(uIDcA.Count() - 1)].ToString());

                int finalHalf = Math.Abs(secondChar - firstChar);

                if (finalHalf <= 5)
                    finalHalf = finalHalf * 2;

                using (var uow = DBHandler.UnitOfWork())
                {
                    uow.UserRatings.CreateUserRating(user.Id, finalHalf);
                    await uow.CompleteAsync();
                }

                await Context.Channel.SendSuccessAsync("User Rating", $"The rating for {user.Mention} is {finalHalf}/10");
            }
        }
        
        [Command("Say"), Summary("Makes the bot say shit")]
        [OwnerOnly]
        public async Task Say(string location, [Remainder] string content)
        {
            string loc = String.Empty;
            if (location.StartsWith("c") || location.StartsWith("C"))
            {
                location = location.Substring(1);
                ulong channelID;
                if (!UInt64.TryParse(location, out channelID))
                {
                    await Context.Channel.SendErrorAsync("Invalid channel");
                    return;
                }

                _logger.Log("Sending to Channel", "Say");
                IMessageChannel channel = (IMessageChannel)await Context.Client.GetChannelAsync(channelID);
                await channel.SendMessageAsync(content);

                loc = channel.Name;
            }

            if (location.StartsWith("u") || location.StartsWith("U"))
            {
                location = location.Substring(1);
                ulong userID;
                if (!UInt64.TryParse(location, out userID))
                {
                    await Context.Channel.SendErrorAsync("Invalid channel");
                    return;
                }

                _logger.Log("Sending to User", "Say");
                IUser User = await Context.Client.GetUserAsync(userID);
                IDMChannel iDMChannel = await User.GetOrCreateDMChannelAsync();
                await iDMChannel.SendMessageAsync(content);

                loc = User.Username + "#" + User.Discriminator;
            }

            if (location.StartsWith("g") || location.StartsWith("G"))
            {
                location = location.Substring(1);
                ulong serverID;
                if (!UInt64.TryParse(location, out serverID))
                {
                    await Context.Channel.SendErrorAsync("Invalid channel");
                    return;
                }

                _logger.Log("Sending to User", "Say");
                IGuild Guild = await Context.Client.GetGuildAsync(serverID);
                IMessageChannel channel = await Guild.GetDefaultChannelAsync();
                await channel.SendMessageAsync(content);

                loc = Guild.Name + "/" + channel.Name;
            }

            await Context.Channel.SendSuccessAsync($"Message sent to {loc}");
        }

        [Command("Quote"), Summary("Will quote a given post ID or the given user's last post in the current channel.")]
        [RequireContext(ContextType.Guild)]
        public async Task Quote(ulong quoteID)
        {
            var Post = await Context.Channel.GetMessageAsync(quoteID);
            await QuotePost(Post, Context.Channel);
        }

        [Command("Quote"), Summary("Will quote a given post ID or the given user's last post in the current channel.")]
        [RequireContext(ContextType.Guild)]
        public async Task Quote(ulong channelID, ulong quoteID)
        {
            var Channel = (IMessageChannel) await Context.Guild.GetChannelAsync(channelID);
            var Post = await Channel.GetMessageAsync(quoteID);
            await QuotePost(Post, Context.Channel);
        }

        [Command("Quote"), Summary("Will quote a given post ID or the given user's last post in the current channel.")]
        [RequireContext(ContextType.Guild)]
        public async Task Quote(IGuildUser user)
        {
            var PostHistory = Context.Channel.GetMessagesAsync();
            var MessageList = PostHistory.First().GetAwaiter().GetResult();
            var Message = MessageList.First(x => x.Author.Id == user.Id);

            await QuotePost(Message, Context.Channel);
        }

        private async Task QuotePost(IMessage post, IMessageChannel channel)
        {
            var embed = new EmbedBuilder().WithQuoteColour().WithAuthor(x => x.WithIconUrl(post.Author.GetAvatarUrl()).WithName(post.Author.Username)).WithDescription(post.Content).WithFooter(x => x.WithText(post.Timestamp.ToString()));
            await channel.BlankEmbedAsync(embed);
        }

        [Command("Attention"), Summary("Give attention to a user.")]
        [Alias("Notice")]
        public async Task Attention(IGuildUser user)
        {
            if (user.Id == Context.User.Id) return;

            Attention UserAttention;
            using (var uow = DBHandler.UnitOfWork())
            {
                UserAttention = uow.Attention.GetOrCreateAttention(Context.User.Id);
            }

            if (UserAttention.LastUsage + new TimeSpan(24, 0, 0) > DateTime.Now)
            {
                if (UserAttention.DailyRemaining <= 0)
                {
                    TimeSpan ts = (UserAttention.LastUsage + new TimeSpan(24, 0, 0)).Subtract(DateTime.Now);
                    await Context.Channel.SendErrorAsync($"You must wait {ts.ToString(@"hh\:mm\:ss")} before you can give someone attention.");
                    return;
                }

                UserAttention.DailyRemaining -= 1;
            }
            else
            {
                UserAttention.LastUsage = DateTime.Now;
                UserAttention.DailyRemaining = 2;
            }

            using (var uow = DBHandler.UnitOfWork())
            {
                uow.Attention.Update(UserAttention);
                uow.Attention.AwardAttention(user.Id, 1);
                await uow.CompleteAsync();
            }

            await Context.Channel.SendSuccessAsync($"{Context.User.Username} has given {user.Mention} attention!");
        }

        [Command("AttentionLB"), Summary("Check the attention leaderboard")]
        [Alias("MostLoved", "ALB", "NoticeLB", "NLB")]
        public async Task AttentionLB(int page = 0)
        {
            if (page != 0)
                page -= 1;

            List<Attention> TopAttention;
            using (var uow = DBHandler.UnitOfWork())
            {
                TopAttention = uow.Attention.GetTop(page);
            }

            if (!TopAttention.Any())
            {
                await Context.Channel.SendErrorAsync($"No users found for page {page + 1}");
                return;
            }

            EmbedBuilder embed = new EmbedBuilder().WithQuoteColour().WithTitle("Attention Leaderboard").WithFooter(efb => efb.WithText($"Page: {page + 1}"));

            foreach (Attention c in TopAttention)
            {
                IGuildUser user = await Context.Guild.GetUserAsync(c.UserID);
                string userName = user?.Username ?? c.UserID.ToString();
                EmbedFieldBuilder efb = new EmbedFieldBuilder().WithName(userName).WithValue(c.AttentionPoints).WithIsInline(true);

                embed.AddField(efb);
            }

            await Context.Channel.BlankEmbedAsync(embed);
        }

        [Command("Notices"), Summary("Check your attention")]
        public async Task Notices(IUser user = null)
        {
            if (user == null)
                user = Context.User;

            Attention a;
            using (var uow = DBHandler.UnitOfWork())
            {
                a = uow.Attention.GetOrCreateAttention(user.Id);
            }

            await Context.Channel.SendSuccessAsync($"{user.Username} has been noticed {a.AttentionPoints} times!");
        }

        [Command("NoticeCD"), Summary("Check your CD")]
        [Alias("NCD")]
        public async Task NoticeCD()
        {
            Attention a;
            using (var uow = DBHandler.UnitOfWork())
            {
                a = uow.Attention.GetOrCreateAttention(Context.User.Id);
            }
                  
            if (a.LastUsage + new TimeSpan(24, 0, 0) > DateTime.Now)
            {
                if (a.DailyRemaining > 0)
                {
                    await Context.Channel.SendSuccessAsync($"You still have {a.DailyRemaining} notices left today!");
                }
                else
                {
                    TimeSpan ts = (a.LastUsage + new TimeSpan(24, 0, 0)).Subtract(DateTime.Now);
                    await Context.Channel.SendErrorAsync($"You still have to wait {ts.ToString(@"hh\:mm\:ss")} before you can notice someone!");
                }               
            }
            else 
            {
                await Context.Channel.SendSuccessAsync($"Your cooldown has reset! You have all 3 back.");
            }
        }
    }
}
