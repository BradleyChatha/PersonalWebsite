<template>
    <vue-carousel ref="carousel" 
                  class="carousel" 
                  :per-page="1" 
                  :mouse-drag="false" 
                  :touch-drag="false" 
                  :autoplay="autoplay"
                  :loop="loop"
                  :autoplay-timeout="autoplayTimeout"
                  pagination-color="#D7D0D0">
        <slide v-for="source in sources"
               :key="source">
            <a :href="source" target="__blank">
                <img :src="source" />
            </a>
        </slide>
    </vue-carousel>
</template>
<script>
    import { Carousel, Slide } from 'vue-carousel';

    export default {
        props: {
            sources: Array,
            autoplay: Boolean,
            loop: Boolean,
            autoplayTimeout: Number
        },
        
        components: {
            "vue-carousel": Carousel,
            Slide
        },
        
        mounted () {
            setTimeout(this.$refs.carousel.computeCarouselWidth, 300) // Otherwise images just don't display initially.
        }
    }
</script>