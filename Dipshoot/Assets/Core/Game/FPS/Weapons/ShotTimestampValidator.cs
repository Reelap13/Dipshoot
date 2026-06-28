using UnityEngine;

namespace Game.Players
{
    public readonly struct ShotTimestampValidation
    {
        public readonly int ClaimedViewTick;
        public readonly int ValidatedViewTick;
        public readonly int QueryTick;
        public readonly int VisualBackTicks;
        public readonly int BiasTicks;
        public readonly int ServerRewindTicks;
        public readonly bool WasClamped;

        public ShotTimestampValidation(
            int claimed_view_tick,
            int validated_view_tick,
            int query_tick,
            int visual_back_ticks,
            int bias_ticks,
            int server_rewind_ticks,
            bool was_clamped)
        {
            ClaimedViewTick = claimed_view_tick;
            ValidatedViewTick = validated_view_tick;
            QueryTick = query_tick;
            VisualBackTicks = visual_back_ticks;
            BiasTicks = bias_ticks;
            ServerRewindTicks = server_rewind_ticks;
            WasClamped = was_clamped;
        }
    }

    public static class ShotTimestampValidator
    {
        public static ShotTimestampValidation Validate(
            int input_tick,
            int claimed_view_tick,
            int server_tick,
            int min_visual_back_ticks,
            int max_visual_back_ticks,
            int bias_ticks,
            int max_server_rewind_ticks)
        {
            int resolved_input_tick = input_tick >= 0 ? input_tick : server_tick;
            int min_back_ticks = Mathf.Max(0, min_visual_back_ticks);
            int max_back_ticks = Mathf.Max(min_back_ticks, max_visual_back_ticks);
            int oldest_view_tick = resolved_input_tick - max_back_ticks;
            int newest_view_tick = resolved_input_tick - min_back_ticks;
            int validated_view_tick = Mathf.Clamp(
                claimed_view_tick,
                oldest_view_tick,
                newest_view_tick);

            int resolved_bias_ticks = Mathf.Max(0, bias_ticks);
            int biased_query_tick = validated_view_tick + resolved_bias_ticks;
            int resolved_server_tick = Mathf.Max(0, server_tick);
            int oldest_server_tick = Mathf.Max(
                0,
                resolved_server_tick - Mathf.Max(0, max_server_rewind_ticks));
            int query_tick = Mathf.Clamp(
                biased_query_tick,
                oldest_server_tick,
                resolved_server_tick);

            return new ShotTimestampValidation(
                claimed_view_tick,
                validated_view_tick,
                query_tick,
                Mathf.Max(0, resolved_input_tick - validated_view_tick),
                resolved_bias_ticks,
                Mathf.Max(0, resolved_server_tick - query_tick),
                validated_view_tick != claimed_view_tick ||
                query_tick != biased_query_tick);
        }
    }
}
