var components=function(e){var t={};function n(r){if(t[r])return t[r].exports;var o=t[r]={i:r,l:!1,exports:{}};return e[r].call(o.exports,o,o.exports,n),o.l=!0,o.exports}return n.m=e,n.c=t,n.d=function(e,t,r){n.o(e,t)||Object.defineProperty(e,t,{enumerable:!0,get:r})},n.r=function(e){"undefined"!=typeof Symbol&&Symbol.toStringTag&&Object.defineProperty(e,Symbol.toStringTag,{value:"Module"}),Object.defineProperty(e,"__esModule",{value:!0})},n.t=function(e,t){if(1&t&&(e=n(e)),8&t)return e;if(4&t&&"object"==typeof e&&e&&e.__esModule)return e;var r=Object.create(null);if(n.r(r),Object.defineProperty(r,"default",{enumerable:!0,value:e}),2&t&&"string"!=typeof e)for(var o in e)n.d(r,o,function(t){return e[t]}.bind(null,o));return r},n.n=function(e){var t=e&&e.__esModule?function(){return e.default}:function(){return e};return n.d(t,"a",t),t},n.o=function(e,t){return Object.prototype.hasOwnProperty.call(e,t)},n.p="",n(n.s=0)}([function(e,t,n){"use strict";n.r(t),t.default={LanguageCard:n(1).default}},function(e,t,n){"use strict";n.r(t);var r=function(){var e=this,t=e.$createElement,n=e._self._c||t;return n("div",{staticClass:"wrapper"},[n("div",{staticClass:"language"},[n("img",{staticClass:"atlas",class:[e.logoClass],attrs:{src:"/img/atlas/index.webp",alt:e.logoClass,"asp-append-version":"true"}}),e._v(" "),n("h2",[e._v(e._s(e.timeUsedString)+" Experience")]),e._v(" "),n("div",{staticClass:"rating"},[n("label",[e._v("Comfort")]),e._v(" "),e._l(5,(function(t){return n("span",{key:t,staticClass:"fa fa-star",class:{checked:t<=e.comfort}})}))],2),e._v(" "),n("div",{staticClass:"rating"},[n("label",[e._v("Knowledge")]),e._v(" "),e._l(5,(function(t){return n("span",{key:t,staticClass:"fa fa-star",class:{checked:t<=e.knowledge}})}))],2)])])};r._withStripped=!0;var o=function(e,t,n,r,o,a,s,i){var l,u="function"==typeof e?e.options:e;if(t&&(u.render=t,u.staticRenderFns=n,u._compiled=!0),r&&(u.functional=!0),a&&(u._scopeId="data-v-"+a),s?(l=function(e){(e=e||this.$vnode&&this.$vnode.ssrContext||this.parent&&this.parent.$vnode&&this.parent.$vnode.ssrContext)||"undefined"==typeof __VUE_SSR_CONTEXT__||(e=__VUE_SSR_CONTEXT__),o&&o.call(this,e),e&&e._registeredComponents&&e._registeredComponents.add(s)},u._ssrRegister=l):o&&(l=i?function(){o.call(this,this.$root.$options.shadowRoot)}:o),l)if(u.functional){u._injectStyles=l;var c=u.render;u.render=function(e,t){return l.call(t),c(e,t)}}else{var d=u.beforeCreate;u.beforeCreate=d?[].concat(d,l):[l]}return{exports:e,options:u}}({props:{logoClass:String,timeStarted:Date,comfort:Number,knowledge:Number},computed:{timeUsedString(){const e=Date.now()-this.timeStarted,t=Math.round(Math.abs(e/864e5));return t>=365?`${Math.round(t/365)} Years`:`${Math.round(t/30)} Months`}}},r,[],!1,null,null,null);o.options.__file="Scripts/components/language_card.vue";t.default=o.exports}]);