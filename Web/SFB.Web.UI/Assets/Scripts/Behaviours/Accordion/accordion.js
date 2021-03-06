/*
 <div id="someId" class="js-accordion" aria-expanded="false" aria-controls="collapseMe">
 
 </div>
 <div id="collapseMe" class="js-collapsed">

 </div>
 */
(function ($) {
    "use strict";
    window.GOVUK = window.GOVUK || {};

    // class Accordion
    function Accordion(options) {
        GOVUK.Collapsible.call(this, options);

        if (!options.accordionGroupId)
            throw new ReferenceError("options.accordionGroupId is not present.");
        this.accordionGroupId = options.accordionGroupId;
        this.accordionChangeHandler = options.accordionChangeHandler;

        if (!this.accordionGroupElement)
            throw new ReferenceError("accordionGroupElement did not resolve using the id: " + options.accordionGroupId);
    }

    // class instance methods
    Accordion.prototype = Object.create(GOVUK.Collapsible.prototype, {
        accordionGroupElement: {
            get: function () {
                return document.getElementById(this.accordionGroupId);
            }
        },
        expanderClickHandler: {
            value: function (e) {
                // don't do anything if we're already expanded
                if (!this.isClosed()) return;

                if (this.accordionChangeHandler) this.accordionChangeHandler();

                // collapse any expanded items in the group
                var callingCollapseId = this.collapseId;
                var accordionGroupId = this.accordionGroupId;
                var $accordionElements = $('.js-accordion.selected', this.accordionGroupElement);
                Array.prototype.forEach.call($accordionElements, function (element) {
                    if (element.id == callingCollapseId) return;
                    // ensure any aria-controls are updated correctly using the Collapsible.setExpanded method
                    new GOVUK.Collapsible({ elementId: element.id, accordionGroupId: accordionGroupId, ignoreEvents: true }).setExpanded(false);
                });

                // expand this item
                GOVUK.Collapsible.prototype.setExpanded.call(this, true);

                // set focus to a control underneath expanded element       
                var link = $(this.getCollapsibleElement()).next().find('.focus-first');
                if (link.length > 0) {
                    setTimeout(function () { link.focus(); }, 50);
                } else {
                    var input = $(this.getCollapsibleElement()).next().find('input');
                    setTimeout(function () { input.first().focus(); }, 50);
                }
            }
        }
    });

    Accordion.prototype.replaceHeadWithButton = function () { };

    // class static methods
    Accordion.bindElements = function (accordionGroupId, accordionChangeHandler) {
        var accordionGroupElement = document.getElementById(accordionGroupId);
        var $accordionElements = $(".js-accordion", accordionGroupElement);

        Array.prototype.forEach.call($accordionElements, function (element) {
            new Accordion({ elementId: element.id, accordionGroupId: accordionGroupId, accordionChangeHandler: accordionChangeHandler });
        });
    };

    GOVUK.Accordion = Accordion;

}(jQuery));

/*-------------------------------------------------------------------------------------------------------------------------------------------------------------
  Accordion

  This allows a collection of sections to be collapsed by default,
  showing only their headers. Sections can be exanded or collapsed
  individually by clicking their headers. An "Open all" button is
  also added to the top of the accordion, which switches to "Close all"
  when all the sections are expanded.

  The state of each section is saved to the DOM via the `aria-expanded`
  attribute, which also provides accessibility.

*/


function Accordion(element) {

  // First do feature detection for required API methods
  if (
    document.querySelectorAll &&
    window.NodeList &&
    'classList' in document.body
  ) {

    this.element = element
    this.sections = []
    this.setup()

  }

}

function AccordionSection(element, accordion) {
  this.element = element
  this.accordion = accordion
  this.setup()
}

Accordion.prototype.setup = function() {

    var accordion_sections = this.element.querySelectorAll('.accordion-section')

    var accordion = this

    for (var i = accordion_sections.length - 1; i >= 0; i--) {
        accordion.sections.push(new AccordionSection(accordion_sections[i], accordion))
    };

    if (this.element.querySelectorAll('.accordion-controls').length > 0) {
        this.element.removeChild(this.element.firstChild);
    }
    var accordion_controls = document.createElement('div')
    accordion_controls.setAttribute('class', 'accordion-controls')

    if (accordion_sections.length > 4) {
        var open_or_close_all_button = document.createElement('button')
        open_or_close_all_button.textContent = 'Open all'
        open_or_close_all_button.setAttribute('aria-label', 'Open all sections')
        open_or_close_all_button.setAttribute('class', 'accordion-expand-all')
        open_or_close_all_button.setAttribute('aria-expanded', 'false')
        open_or_close_all_button.setAttribute('type', 'button')

        open_or_close_all_button.addEventListener('click', this.openOrCloseAll.bind(this))

        accordion_controls.appendChild(open_or_close_all_button)
    }
    this.element.insertBefore(accordion_controls, this.element.firstChild)

  this.element.classList.add('with-js')
}

Accordion.prototype.openOrCloseAll = function(event) {

  var open_or_close_all_button = event.target
  var now_expanded = !(open_or_close_all_button.getAttribute('aria-expanded') == 'true')

  for (var i = this.sections.length - 1; i >= 0; i--) {
    this.sections[i].setExpanded(now_expanded)
  };

  this.setOpenCloseButtonExpanded(now_expanded)

}


Accordion.prototype.setOpenCloseButtonExpanded = function(expanded) {

  var open_or_close_all_button = this.element.querySelector('.accordion-expand-all')

  var new_button_text = expanded ? "Close all" : "Open all"
    var new_button_aria_label = expanded ? "Close all sections" : "Open all sections"
    if (open_or_close_all_button) {
        open_or_close_all_button.setAttribute('aria-expanded', expanded)
        open_or_close_all_button.setAttribute('aria-label', new_button_aria_label)
        open_or_close_all_button.textContent = new_button_text
    }

}

Accordion.prototype.updateOpenAll = function() {

  var sectionsCount = this.sections.length

  var openSectionsCount = 0

  for (var i = this.sections.length - 1; i >= 0; i--) {
    if (this.sections[i].expanded()) {
      openSectionsCount += 1
    }
  };

  if (sectionsCount == openSectionsCount) {
    this.setOpenCloseButtonExpanded(true)
  } else {
    this.setOpenCloseButtonExpanded(false)
  }

}

AccordionSection.prototype.setup = function () {
  this.element.setAttribute('aria-expanded', 'false')

  var header = this.element.querySelector('.accordion-section-header')
  header.addEventListener('click', this.toggleExpanded.bind(this))
  header.addEventListener('keypress', this.keyPressed.bind(this))
  header.setAttribute('aria-expanded', 'false')
  //header.setAttribute('tabindex', '0')
  //header.setAttribute('role', 'button')

  //var button = this.element.querySelector('.chart-accordion-header')
  //button.setAttribute('aria-label', button.getAttribute('aria-label') + ' Show section')


  var icon = document.createElement('span')
  icon.setAttribute('class', 'icon accordion-icon')

  header.appendChild(icon)
}

AccordionSection.prototype.keyPressed = function(event) {
        
        if (event.key === " " || event.key === "Enter") {
        event.preventDefault();
        this.toggleExpanded();
        }
    }

AccordionSection.prototype.toggleExpanded = function(){
  var expanded = (this.element.getAttribute('aria-expanded') == 'true')

  this.setExpanded(!expanded)
  this.accordion.updateOpenAll()
}

AccordionSection.prototype.expanded = function() {
  return (this.element.getAttribute('aria-expanded') == 'true')
}

AccordionSection.prototype.setExpanded = function (expanded) {
    this.element.setAttribute('aria-expanded', expanded)
    this.element.querySelector('.accordion-section-header').setAttribute('aria-expanded', expanded)

    var button = this.element.querySelector('.chart-accordion-header')
    if (button) {
        if (expanded) {
            button.setAttribute('aria-label', button.getAttribute('aria-label').replace("show", "hide"))
        } else {
            button.setAttribute('aria-label', button.getAttribute('aria-label').replace("hide", "show"))
        }
    }

    // This is set to trigger reflow for IE8, which doesn't
  // always reflow after a setAttribute call.
  this.element.className = this.element.className

}