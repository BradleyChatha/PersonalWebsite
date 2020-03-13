<template>
    <div class="wrapper">
        <div class="language">
            <img class="atlas" v-bind:class="[logoClass]" src="/img/atlas/index.webp" :alt="logoClass" asp-append-version="true" />
            <h2>{{ timeUsedString }} Experience</h2>
            <div class="rating">
                <label>Comfort</label>
                <span v-for="i in 5" class="fa fa-star" v-bind:class="{ checked: i <= comfort }" :key="i"></span>
            </div>
            <!--A bit of code duplication, but meh-->
            <div class="rating">
                <label>Knowledge</label>
                <span v-for="i in 5" class="fa fa-star" v-bind:class="{ checked: i <= knowledge }" :key="i"></span>
            </div>
        </div>
    </div>
</template>

<script>
export default {
    props: {
        logoClass: String,
        timeStarted: Date,
        comfort: Number,
        knowledge: Number
    },
    computed: {
        timeUsedString() {
            const elapsed = Date.now() - this.timeStarted;
            const oneDayMs = 24 * 60 * 60 * 1000;
            const daysElapsed = Math.round(Math.abs(elapsed / oneDayMs));

            return (daysElapsed >= 365)
                ? `${Math.round(daysElapsed / 365)} Years`
                : `${Math.round(daysElapsed / 30)} Months`;
        }
    }
}
</script>