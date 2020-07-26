<template>
    <div class="wrapper">
        <div class="language" ref="card" :style="{ height: cardHeight }" @transitionend="onCardResize">
            <img class="atlas" v-bind:class="[logoClass]" src="/img/atlas/index.webp" :alt="logoClass" asp-append-version="true" />
            <h2>{{ timeUsedString }}' {{ experienceLabel }}</h2>
            <div class="rating">
                <label>Comfort</label>
                <span v-for="i in 5" class="fa fa-star" v-bind:class="{ checked: i <= comfort }" :key="i"></span>
            </div>
            <!--A bit of code duplication, but meh-->
            <div class="rating">
                <label>Knowledge</label>
                <span v-for="i in 5" class="fa fa-star" v-bind:class="{ checked: i <= knowledge }" :key="i"></span>
            </div>
            <div v-if="metadata && metadata.length > 0 && metadataShow && metadataShowContent" class="metadata" ref="metadata">
                <div class="content" 
                     :class="{ processed: (metadataHeight > 0), shown: !metadataFadeout, hidden: metadataFadeout }"
                     @animationend="onMetadataFadeout">
                    <div v-for="item in metadata"
                        :key="item">
                        <i class="fa fa-check"></i>
                        {{ item }}
                    </div>
                </div>
            </div>
            <div v-if="metadata && metadata.length > 0" class="toggle" :class="metadataShow ? 'up' : 'down'" @click="onToggleMetadata">
                <i class="fa fa-chevron-right"></i>
            </div>
        </div>
    </div>
</template>

<script>
export default {
    data() {
        return {
            metadataHeight: 0,
            metadataShow: true,
            metadataShowContent: true,
            metadataFadeout: false,
            baseHeight: 0
        };
    },

    props: {
        logoClass: String,
        timeStarted: Date,
        comfort: Number,
        knowledge: Number,
        metadata: Array,
        experienceLabel: String
    },
    
    computed: {
        timeUsedString() {
            const elapsed = Date.now() - this.timeStarted;
            const oneDayMs = 24 * 60 * 60 * 1000;
            const daysElapsed = Math.round(Math.abs(elapsed / oneDayMs));

            return (daysElapsed >= 365)
                ? `${Math.round(daysElapsed / 365)} Years`
                : `${Math.round(daysElapsed / 30)} Months`;
        },

        cardHeight() {
            const height = (this.metadataShow ? this.baseHeight + this.metadataHeight : this.baseHeight);

            return (height == 0) ? "auto" : height + "px";
        }
    },

    methods: {
        onToggleMetadata() {
            if(!this.metadataShow && this.baseHeight == 0)
                this.baseHeight = this.$refs.card.getBoundingClientRect().height;

            if(!this.metadataShow) {
                this.metadataFadeout = false;
                this.metadataShow = true;
            }
            else
                this.metadataFadeout = true; // Will trigger onMetadataFadeout
        },

        onMetadataFadeout() {
            if(this.metadataShow && this.metadataFadeout)
                this.metadataShow = false;
        },

        onCardResize() {
            this.metadataShowContent = (this.$refs.card.getBoundingClientRect().height >= this.baseHeight + this.metadataHeight);
            this.metadataFadeout = false;
        }
    },
    
    mounted() {
        this.metadataShowContent = false;
        if(this.metadata) {
            this.metadataHeight = this.$refs.metadata.getBoundingClientRect().height;
            this.metadataShow = false;
        }

        this.baseHeight = 313; // Hard coded because it's so annoying to set this automatically, witout the first-time animation breaking.
                               // Will have to keep this manually updated, but thankfully that shouldn't happen too often.
    }
}
</script>