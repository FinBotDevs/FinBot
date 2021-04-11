# GOOD CODE:
import discord
from discord.ext import commands
from main import FinBot
from pytube import Playlist
from Handlers.storageHandler import DataHelper
import youtube_dl
import asyncio
from functools import partial
import aiohttp
import json.decoder
import re
import time
from Data import config

# BAD CODE:

import spotipy
from youtube_dl import YoutubeDL
from urllib import request as rq
from argparse import ArgumentParser
from spotipy.oauth2 import SpotifyClientCredentials

import random as rand


# BAD CODE:
parser = ArgumentParser(description="Download Spotify playlist the easy way")


class Hades:
    # Spotify app credentials
    __CLIENT_ID = config.client_Id
    __CLIENT_SECRET = config.client_secret

    def __init__(self, pl_uri, embed):
        self.auth_manager = SpotifyClientCredentials(
            client_id=self.__CLIENT_ID, client_secret=self.__CLIENT_SECRET
        )
        self.sp = spotipy.Spotify(auth_manager=self.auth_manager)
        self.pl_uri = pl_uri
        self.embed = embed

    def get_ydl_opts(self, path):
        if self.embed:
            return {
                "writethumbnail": True,
                "format": "bestaudio/best",
                "outtmpl": f"./{path}/%(title)s.%(ext)s",
                "postprocessors": [
                    {
                        "key": "FFmpegExtractAudio",
                        "preferredcodec": "mp3",
                        "preferredquality": "320",
                    },
                    {
                        "key": "EmbedThumbnail",
                    },
                ],
            }
        else:
            return {
                "format": "bestaudio/best",
                "outtmpl": f"./{path}/%(title)s.%(ext)s",
                "postprocessors": [
                    {
                        "key": "FFmpegExtractAudio",
                        "preferredcodec": "mp3",
                        "preferredquality": "320",
                    }
                ],
            }

    def get_playlist_details(self):
        offset = 0
        pl_name = self.sp.playlist(self.pl_uri)["name"]
        pl_items = self.sp.playlist_items(
            self.pl_uri,
            offset=offset,
            fields="items.track.name,items.track.artists.name, total",
            additional_types=["track"],
        )["items"]

        pl_tracks = []
        while len(pl_items) > 0:
            for item in pl_items:
                track_name = item["track"]["name"].replace(" ", "+")
                artist_name = item["track"]["artists"][0]["name"].replace(" ", "+")
                pl_tracks.append(f"{track_name}+{artist_name}".encode("utf8"))

            offset = (offset + len(pl_items))
            pl_items = self.sp.playlist_items(
                self.pl_uri,
                offset=offset,
                fields="items.track.name,items.track.artists.name, total",
                additional_types=["track"],
            )["items"]

        return {"pl_name": pl_name, "pl_tracks": pl_tracks}

    def create_download_directory(self, dir_name):
        path = f"./{dir_name}"

        if os.path.exists(path):
            return path

        try:
            os.mkdir(path)
            return path
        except OSError:
            print("Creation of the download directory failed")

    def download_tracks(self):
        pl_details = self.get_playlist_details()
        path = self.create_download_directory(pl_details["pl_name"])

        with YoutubeDL(self.get_ydl_opts(path)) as ydl:
            for track in pl_details["pl_tracks"]:
                html = rq.urlopen(
                    f"https://www.youtube.com/results?search_query={track}"
                )
                video_ids = re.findall(r"watch\?v=(\S{11})", html.read().decode())

                if video_ids:
                    url = "https://www.youtube.com/watch?v=" + video_ids[0]
                    ydl.download([url])




# GOOD CODE:

youtube_dl.utils.bug_reports_message = lambda: ''

ytdl_format_options = {
    'format': 'bestaudio/best',
    'outtmpl': '%(extractor)s-%(id)s-%(title)s.%(ext)s',
    'restrictfilenames': True,
    'noplaylist': True,
    'nocheckcertificate': True,
    'ignoreerrors': False,
    'logtostderr': False,
    'quiet': True,
    'no_warnings': True,
    'default_search': 'auto',
    'source_address': '0.0.0.0'  # bind to ipv4 since ipv6 addresses cause issues sometimes
}

ffmpeg_options = {
    'before_options': '-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5',
    'options': '-vn'
}

ytdl = youtube_dl.YoutubeDL(ytdl_format_options)


class YTDLSource(discord.PCMVolumeTransformer):
    def __init__(self, source, *, data, volume=0.5, resume_from=0):
        super().__init__(source, volume)
        self.data = data
        self.time = 0.0
        self.title = data.get('title')
        self.url = data.get('url')
        self.webpage_url = data.get("webpage_url")
        self.start_time = None
        self.resume_from = resume_from

    def read(self):
        if not self.start_time:
            self.start_time = time.time() - self.resume_from
        return super().read()

    @classmethod
    async def from_url(cls, url, *, loop=None, stream=False):
        data = await cls.get_video_data(url, loop)
        if data is None:
            return None
        return cls(discord.FFmpegPCMAudio(data["url"], **ffmpeg_options), data=data)

    @staticmethod
    async def get_video_data(url, loop=None):
        loop = loop or asyncio.get_event_loop()
        try:
            data = await loop.run_in_executor(None, lambda: ytdl.extract_info(url, download=False))
        except youtube_dl.utils.DownloadError:
            return None
        if 'entries' in data and len(data['entries']) > 0:
            # take first item from a playlist
            data = data['entries'][0]
        if data.get('url', None) is None:
            return None
        return data


class Music(commands.Cog):
    def __init__(self, bot: FinBot):
        self.bot = bot
        self.data = DataHelper()
        self.data["song_queues"] = {}
        self.called_from = {}

    def enqueue(self, guild, song_url, time=None, start=False):
        all_queues = self.data.get("song_queues", {})
        guild_queue: list = all_queues.get(str(guild.id), [])
        if time is None:
            to_queue = song_url
        else:
            to_queue = [song_url, time]
        if start:
            guild_queue.insert(0, to_queue)
        else:
            guild_queue.append(to_queue)
        all_queues[str(guild.id)] = guild_queue

        self.data["song_queues"] = all_queues
        return True

    @staticmethod
    def get_playlist(provided_info):
        try:
            playlist_info = Playlist(provided_info).video_urls
        except KeyError:
            playlist_info = None
        return playlist_info

    async def title_from_url(self, video_url):
        params = {"format": "json", "url": video_url}
        url = "https://www.youtube.com/oembed"
        async with aiohttp.ClientSession() as session:
            request = await session.get(url=url, params=params)
            try:
                json_response = await request.json()
            except json.decoder.JSONDecodeError:
                json_response = await YTDLSource.get_video_data(video_url, self.bot.loop)
        return json_response["title"]

    def thumbnail_from_url(self, video_url):
        exp = r"^.*((youtu.be\/)|(v\/)|(\/u\/\w\/)|(embed\/)|(watch\?))\??v?=?([^#&?]*).*"
        s = re.findall(exp, video_url)[0][-1]
        thumbnail = f"https://i.ytimg.com/vi/{s}/hqdefault.jpg"
        return thumbnail

    @commands.command()
    async def play(self, ctx, *, to_play):
        async with ctx.typing():
            playlist_info = await self.bot.loop.run_in_executor(None, partial(self.get_playlist, to_play))
            if playlist_info is None:
                playlist_info = await YTDLSource.get_video_data(to_play, self.bot.loop)
                playlist_info = [playlist_info["webpage_url"]]
            first_song = playlist_info.pop(0)
            self.enqueue(ctx.guild, first_song)
            self.called_from[ctx.guild.id] = ctx.channel
            if not ctx.voice_client.is_playing():
                self.bot.loop.create_task(self.play_next_queued(ctx.voice_client))
            first_song_name = await self.title_from_url(first_song)
            embed = self.bot.create_completed_embed("Added song to queue!", f"Added [{first_song_name}]"
                                                                            f"({first_song}) "
                                                                            f"to queue!\n"
                                                                            f"Please note other songs in "
                                                                            f"a playlist may still be "
                                                                            f"processing.")
            embed.set_thumbnail(url=self.thumbnail_from_url(first_song))
            await ctx.reply(embed=embed)
            futures = []
            for url in playlist_info:
                futures.append(self.bot.loop.create_task(self.title_from_url(url), name=url))
            await asyncio.sleep(2)
            titles = await asyncio.gather(*futures)
            successfully_added = ""
            for index, title in enumerate(titles):
                self.enqueue(ctx.guild, playlist_info[index])
                successfully_added += f"{index + 1}. **{title}**\n"
        if successfully_added != "":
            for short_text in self.bot.split_text(successfully_added):
                await ctx.reply(embed=self.bot.create_completed_embed("Successfully queued songs!", short_text))

    async def play_next_queued(self, voice_client: discord.VoiceClient):
        if voice_client is None or not voice_client.is_connected():
            return
        while voice_client.is_playing():
            await asyncio.sleep(0.5)
        await asyncio.sleep(1)
        all_queued = self.data.get("song_queues", {})
        guild_queued = all_queued.get(str(voice_client.guild.id), [])
        if len(guild_queued) == 0:
            # await voice_client.disconnect()
            return
        next_song_url = guild_queued.pop(0)
        local_ffmpeg_options = ffmpeg_options.copy()
        resume_from = 0
        if type(next_song_url) == tuple or type(next_song_url) == list:
            next_song_url, resume_from = next_song_url
            local_ffmpeg_options['options'] = "-vn -ss {}".format(resume_from)
        print(next_song_url)
        all_queued[str(voice_client.guild.id)] = guild_queued
        self.data["song_queues"] = all_queued
        volume = self.data.get("song_volumes", {}).get(str(voice_client.guild.id), 0.5)
        data = await YTDLSource.get_video_data(next_song_url, self.bot.loop)
        source = YTDLSource(discord.FFmpegPCMAudio(data["url"], **local_ffmpeg_options),
                            data=data, volume=volume, resume_from=resume_from)
        voice_client.play(source, after=lambda e: self.bot.loop.create_task(self.play_next_queued(voice_client)))
        title = await self.title_from_url(next_song_url)
        embed = self.bot.create_completed_embed("Playing next song!", "Playing **[{}]({})**".format(title,
                                                                                                    next_song_url))
        embed.set_thumbnail(url=self.thumbnail_from_url(next_song_url))
        history = await self.called_from[voice_client.guild.id].history(limit=1).flatten()
        await self.called_from[voice_client.guild.id].send(embed=embed)

    @commands.command()
    async def resume(self, ctx):
        self.bot.loop.create_task(self.play_next_queued(ctx.voice_client))
        await ctx.reply(embed=self.bot.create_completed_embed("Resumed!", "Resumed playing."))

    @commands.command(aliases=["stop"])
    async def pause(self, ctx):
        currently_playing_url = ctx.voice_client.source.webpage_url
        current_time = int(time.time() - ctx.voice_client.source.start_time)
        self.enqueue(ctx.guild, currently_playing_url, int(current_time), start=True)
        ctx.voice_client.stop()
        await ctx.voice_client.disconnect()
        await ctx.reply(embed=self.bot.create_completed_embed("Successfully paused.", "Song paused successfully."))

    @commands.command()
    async def skip(self, ctx):
        ctx.voice_client.stop()
        await ctx.reply(embed=self.bot.create_completed_embed("Song skipped.", "Song skipped successfully."))

    @commands.command()
    async def volume(self, ctx, volume: float):
        if volume > 1:
            volume = volume / 100
        elif volume < 0:
            volume = 0
        all_guilds = self.data.get("song_volumes", {})
        all_guilds[str(ctx.guild.id)] = volume
        self.data["song_volumes"] = all_guilds
        ctx.voice_client.source.volume = volume
        await ctx.reply(embed=self.bot.create_completed_embed("Changed volume!", f"Set volume to "
                                                                                 f"{volume * 100}% for this guild!"))

    # async def queue(self, ctx):
    #     self.bot.add_listener()
    #     guild_queue = self.data.get("song_queues", {}).get(str(ctx.guild.id), [])
    #     queue_message = ""
    #     for index in range(len(guild_queue)):
    #         link = guild_queue[index]
    #         if index % 5 == 0:

    @commands.command()
    async def mute(self, ctx):
        all_guilds = self.data.get("song_volumes", {})
        all_guilds[str(ctx.guild.id)] = 0
        self.data["song_volumes"] = all_guilds
        ctx.voice_client.source.volume = 0
        await ctx.reply(embed=self.bot.create_completed_embed(f"{config.mute_emoji}Muted bot!", f"muted bot for this "
                                                                                                f"guild!\nTo undo this,"
                                                                                                f" just type "
                                                                                                f"{config.prefix}volume"
                                                                                                f" <new volume>"))

    @commands.command()
    async def unmute(self, ctx):
        all_guilds = self.data.get("song_volumes", {})
        all_guilds[str(ctx.guild.id)] = 100
        self.data["song_volumes"] = all_guilds
        ctx.voice_client.source.volume = 1-0
        await ctx.reply(embed=self.bot.create_completed_embed(f"unmuted bot!", "successfully unmuted the bot!"))

    @commands.command()
    async def shuffle(self, ctx):
        voice_client = ctx.voice_client
        all_queued = self.data.get("song_queues", {})
        guild_queued = all_queued.get(str(voice_client.guild.id), [])
        rand.shuffle(guild_queued)
        all_queued[str(voice_client.guild.id)] = guild_queued
        await ctx.reply(embed=self.bot.create_completed_embed("Shuffled the current playlist", "current guild playlist"
                                                                                               " shuffled!"))



    @commands.command()
    async def testing(self, ctx, *, to_play):
        parser.add_argument(
            "playlist_uri", metavar="PL_URI", type=str, help="Spotify playlist uri"
        )

        parser.add_argument('-e', '--embed', action='store_true',
                            help='embeds youtube thumbnail into mp3')

        args = parser.parse_args()
        hades = Hades(args.playlist_uri, args.embed)
        hades.download_tracks()
        # for playlist in Hades(to_play, None).get_playlist_details():
        #     for track in playlist["pl_tracks"]:
        #         queue.add(track)

    @shuffle.before_invoke
    @unmute.before_invoke
    @mute.before_invoke
    @volume.before_invoke
    @pause.before_invoke
    @play.before_invoke
    @resume.before_invoke
    @skip.before_invoke
    async def ensure_voice(self, ctx):
        if ctx.voice_client is None:
            if ctx.author.voice:
                await ctx.author.voice.channel.connect()
            else:
                await ctx.reply(embed=self.bot.create_error_embed("You are not connected to a voice channel."))
                raise commands.CommandError("Author not connected to a voice channel.")
        elif not ctx.author.voice or ctx.voice_client.channel != ctx.author.voice.channel:
            await ctx.reply(embed=self.bot.create_error_embed("You have to be connected to the voice channel to "
                                                              "execute these commands!"))
            raise commands.CommandError("Author not connected to the correct voice channel.")


def setup(bot: FinBot):
    cog = Music(bot)
    bot.add_cog(cog)
